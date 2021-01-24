using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BikeControlV2 : MonoBehaviour
{
    [Header("Movements Settings")]

    [SerializeField] float forwardSpeed = 100;
    [SerializeField] float backwardSpeed = 20;
    [SerializeField] float steerSpeed = 3;
    [SerializeField] float wheelyForce = 1;
    [SerializeField] float minSpeed = 100;


    [Header("Render Settings")]

    [SerializeField] GameObject handlebar;
    [SerializeField] float steerAngle = 30;

    [SerializeField] GameObject bike;
    [SerializeField] float balanceAngle = 10;

    [SerializeField] GameObject[] wheels;
    [SerializeField] float wheelCoefSpeed = 1;

    [SerializeField] ParticleSystem dust;
    [SerializeField] TrailRenderer[] gums;
    [SerializeField] ParticleSystem[] exhaustpipe;

    [SerializeField] Light[] lights;
    bool lightOnOff = false;
    bool grounded;

    [SerializeField] bool enableUI = true;
    [SerializeField] Canvas canvas;

    int sense;

    [Header("GearBox Settings")]

    [SerializeField] bool automaticGearbox;
    [SerializeField] Toggle modeDisplay;
    [SerializeField] int highestGear = 5;
    [SerializeField] int currentGear = 0;
    [SerializeField] Text GearboxDisplay;
    [SerializeField] float gearSpeedMultiplier;

    [SerializeField] float nextGeartime = 2;
    [SerializeField] float previousGeartime = 1;
    float nextGear;

    [Header("Physics Settings")]

    Rigidbody rigid;
    [SerializeField] float mass = 10;
    [SerializeField]float flyingMass;

    void Start()
    {
        rigid = GetComponent<Rigidbody>();
    }

    
    void FixedUpdate()
    {
        Movement();
        Rendering();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            automaticGearbox = !automaticGearbox;
            modeDisplay.isOn = automaticGearbox;
        }
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            enableUI = !enableUI;
            canvas.enabled = enableUI;
        }

        GearBox();
    }

    void Movement()
    {
        float v = Input.GetAxis("Vertical");
        int layermask = 1 << 9;
        if (Physics.Raycast(transform.position, -transform.up, 1f, ~layermask))
        {
            //forward
            //if (v > 0) rigid.AddRelativeForce(Vector3.forward * forwardSpeed * v, ForceMode.Force);

            //backward
            //if (v < 0) rigid.AddRelativeForce(Vector3.forward * backwardSpeed * v, ForceMode.Force);

            //backward & forward
            if (v != 0) rigid.AddRelativeForce(Vector3.forward * forwardSpeed, ForceMode.Force);

            //light mass
            rigid.mass = mass;
            rigid.drag = 1f;
        }
        else
        {
            //heavy mass
            rigid.mass = flyingMass;
            rigid.drag = 0.1f;
        }

        //wheely
        if (v != 0)
        {
            rigid.AddRelativeTorque(Vector3.right * -wheelyForce * v, ForceMode.Force);
        }

        float h = Input.GetAxis("Horizontal");
        if (h != 0 && rigid.velocity != Vector3.zero && v != 0)
        {
            rigid.AddRelativeTorque(new Vector3(0, transform.rotation.y + h * steerSpeed, transform.rotation.z), ForceMode.Acceleration);
        }
    }

    void Rendering()
    {
        //renderer control
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (h != 0)
        {
            handlebar.transform.localRotation = Quaternion.Euler(90, transform.rotation.y + h * steerAngle, transform.rotation.z);
            if (rigid.velocity != Vector3.zero)
            {
                bike.transform.localRotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, transform.rotation.z + h * -balanceAngle * v);
            }
        }
        else
        {
            bike.transform.localRotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, transform.rotation.z);
        }
        if (rigid.velocity != Vector3.zero)
        {
            foreach (GameObject wheel in wheels)
            {
                if(v != 0)  wheel.transform.Rotate(transform.rotation.x + wheelCoefSpeed * sense, 0, 0);
            }
        }

        Quaternion angle = transform.localRotation;
        if (h == 0)
        {

            angle.z = 0;
            if (angle.x > 70 || angle.x < -70) angle.x = 0;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, angle, 100f);
            bike.transform.rotation = Quaternion.Slerp(bike.transform.rotation, transform.rotation, 1);
        }
        angle.z = 0;
        transform.localRotation = angle;

        //dust control
        for (int i = 0; i < gums.Length; i++)
        {
            grounded = Physics.Raycast(wheels[i].transform.position, -transform.up, 1.2f, 1 << 10);
            Debug.DrawRay(wheels[i].transform.position, -transform.up * 1.2f);
            var main = dust.main;
            if (grounded)
            {
                gums[i].emitting = true;
                if (v != 0)
                {
                    dust.Play();
                }
                else
                {
                    main.loop = true;
                    main.loop = false;
                }
            }
            else
            {
                main.loop = false;
                gums[i].emitting = false;
            }
        }

        //light control
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (lightOnOff)
            {
                foreach (Light light in lights) light.enabled = false;
                lightOnOff = false;
            }
            else
            {
                foreach (Light light in lights) light.enabled = true;
                lightOnOff = true;
            }
        }

        //exhaustPipes
        if(currentGear != 0)
        {
            foreach(ParticleSystem particle in exhaustpipe)
            {
                particle.Play();
            }
        }
    }

    void GearBox()
    {
        float v = Input.GetAxis("Vertical");
        if (automaticGearbox)
        {
            if (v > 0 && currentGear <= 0)
            {
                currentGear = 1;
                nextGeartime = 2;
                nextGear = Time.time + nextGeartime;
                GearboxDisplay.text = currentGear.ToString();
                forwardSpeed = gearSpeedMultiplier * currentGear + minSpeed;
            }
            if (v > 0 && Time.time > nextGear && nextGeartime > 0)
            {
                if (currentGear < highestGear)
                {
                    nextGear = Time.time + nextGeartime;
                    nextGeartime *= 1.2f;
                    currentGear++;
                    GearboxDisplay.text = currentGear.ToString();
                }
            }
            else if (v == 0 && Time.time > nextGear && previousGeartime > 0)
            {
                if (currentGear > 0)
                {
                    nextGear = Time.time + previousGeartime;
                    nextGeartime /= 1.25f;
                    currentGear--;
                    GearboxDisplay.text = currentGear.ToString();
                }
            }
            else if (v < 0)
            {
                currentGear = -1;
                GearboxDisplay.text = "R";
            }

            if (currentGear > 0)
            {
                forwardSpeed = gearSpeedMultiplier * currentGear + minSpeed;
            }
            else
            {
                forwardSpeed = gearSpeedMultiplier * currentGear - minSpeed;
            }
        }
        else
        {
            if (v > 0 && Input.GetKeyDown(KeyCode.E) && currentGear < 5)
            {
                currentGear++;
                GearboxDisplay.text = currentGear.ToString();
            }
            if (Input.GetKeyDown(KeyCode.A) && currentGear > 0)
            {
                currentGear--;
                GearboxDisplay.text = currentGear.ToString();
            }
            else if(Input.GetKeyDown(KeyCode.A))
            {
                currentGear = -1;
                GearboxDisplay.text = "R";
            }

            if(currentGear > 0)
            {
                forwardSpeed = gearSpeedMultiplier * currentGear + minSpeed;
            }
            else
            {
                forwardSpeed = gearSpeedMultiplier * currentGear - minSpeed;
            }
        }

        if (currentGear < 0) sense = -1;
        else sense = 1;

        if (currentGear == 0)
        {
            forwardSpeed = 0;
            sense = 0;
        }
    }
}
