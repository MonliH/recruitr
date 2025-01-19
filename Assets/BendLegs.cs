using UnityEngine;

public class BendLegs : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.Find("Armature/Hips/LeftUpLeg").Rotate(new Vector3(85f, 9f, 0f));
        transform.Find("Armature/Hips/RightUpLeg").Rotate(new Vector3(75f, -9f, 0f));
        transform.Find("Armature/Hips/LeftUpLeg/LeftLeg").Rotate(new Vector3(-95f, 0f, 0f));
        transform.Find("Armature/Hips/RightUpLeg/RightLeg").Rotate(new Vector3(-90f, 0f, 0f));
        transform.Find("Armature/Hips/Spine/Spine1/Spine2/LeftShoulder").Rotate(new Vector3(-10, 0f, 0f));
        transform.Find("Armature/Hips/Spine/Spine1/Spine2/RightShoulder").Rotate(new Vector3(-10, 0f, 0f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
