using UnityEngine;

public class JoystickCut : MonoBehaviour
{
    public ClothCutter cutterBehavior;

    // Update is called once per frame
    void Update()
    {
        bool shouldCutInput = Input.GetButton("Fire1");
        cutterBehavior.Cutting = shouldCutInput;
    }
}
