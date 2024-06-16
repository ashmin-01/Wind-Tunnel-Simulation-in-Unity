using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class collisionTest : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // Code to execute when collision occurs
        Debug.Log("is Collision detected ");
    }
}
