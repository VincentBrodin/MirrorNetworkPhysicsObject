using Mirror;
using UnityEngine;

namespace Networking{
    [RequireComponent(typeof(NetworkIdentity))]
    public class NetworkPhysicsObject : NetworkBehaviour{
        public enum UpdateType{
            Override, //Override will override any offset between the server and client
            Addition, // Addition will add any offset to make the object smoother
        }

        public new Rigidbody rigidbody;
        public UpdateType updateType;

        //The servers 
        private float _nextSend;

        //How often the server will be sending updates
        private const float SendRate = 0.0166666666666667f; // = 1/SendsPerSecond
        private const int SendsPerSecond = 60; // = 1/SendRate

        //Makes sure that the player has everything before getting new updates from the server
        private bool _hasGottenSetUp;

        //The last time the player got an update
        private float _lastSendFromServer;

        //Where the object is on the server
        private Vector3 _goalPosition;
        private Quaternion _goalRotation;

        //Where the object started on the client
        private Vector3 _startPosition;
        private Quaternion _startRotation;

        //The target transform  is cached to stop calls to the C++ backend of unity
        private Transform _transform;


        private void Start(){
            SetUp();
        }

        private void SetUp(){
            if (rigidbody == null){
                rigidbody = GetComponent<Rigidbody>();
            }

            if (isServer){
                //The object is on the server
                rigidbody.isKinematic = false;
            }
            else{
                //The object is on a client
                rigidbody.isKinematic = true;
            }

            _transform = transform;

            _hasGottenSetUp = true;
        }

        private void Update(){
            if (isServer){
                //If the code is running on the server we will send the information
                if (_nextSend > Time.time) return;

                _nextSend = Time.time + SendRate;

                SendData(_transform.position, _transform.rotation);
            }
            else{
                //We will apply our interpolation function to the object
                //Gets the percentage of the send we are currently working with
                float p = Mathf.Clamp01((Time.time - _lastSendFromServer) * SendsPerSecond);

                _transform.position = Vector3.Lerp(_startPosition, _goalPosition, p);
                _transform.rotation = Quaternion.Lerp(_startRotation, _goalRotation, p);
            }

        
        }

        [Command(requiresAuthority = false)]
        private void SendData(Vector3 position, Quaternion rotation){
            //Make sure we don't update the servers information since that will make the object behave unwanted
            ClientSendData(position, rotation);
        }

        [ClientRpc]
        private void ClientSendData(Vector3 position, Quaternion rotation){
            //Make sure we don't update the servers information since that will make the object behave unwanted
            if (isServer) return;

            if (!_hasGottenSetUp)
                SetUp();

            _lastSendFromServer = Time.time;

            if (updateType == UpdateType.Override){
                _startPosition = _transform.position;
                _startRotation = _transform.rotation;
            }
            else if (updateType == UpdateType.Addition){
                _startPosition = _goalPosition;
                _startRotation = _goalRotation;
            }

            _goalPosition = position;
            _goalRotation = rotation;
        }


        protected override void OnValidate(){
            base.OnValidate();

            if (rigidbody == null)
                rigidbody = GetComponent<Rigidbody>();
        }
    }
}
