using UnityEngine;

public class IceCreamMouthSpinner : MonoBehaviour
{
    [SerializeField] private Transform center;
    [SerializeField] private Transform mouth;
    [Tooltip("Rotations per seconds")]
    [SerializeField] private float rotateSpeed;
    [SerializeField] private float closeMoveSpeed;
    [SerializeField] private IceCreamCreator creator;
    private float _radius;
    private float _angle;
    
    private void Start()
    {
        mouth.transform.Translate(0,center.transform.position.y-mouth.transform.position.y,0);
        _radius = Vector3.Distance(mouth.transform.position, center.transform.position);
    }

    private void Update()
    {
        if (creator.GetState()==IceCreamCreator.MachineState.None||creator.GetState()==IceCreamCreator.MachineState.Filled)
        {
            return;
        }
        
        if (_radius<.1)
        {
            creator.ChangeState(IceCreamCreator.MachineState.Filled);
            return;
        }

        mouth.transform.position = new Vector3(_radius*Mathf.Cos(_angle*2*Mathf.PI),mouth.transform.position.y,_radius*Mathf.Sin(_angle*2*Mathf.PI));
        _angle += rotateSpeed * Time.deltaTime;
        _radius -= closeMoveSpeed * Time.deltaTime;

    }
}
