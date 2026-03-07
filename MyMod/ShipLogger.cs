
using System;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Linq;

namespace ShipLogger
{
    public class ShipLogger : ModBehaviour
    {
        private bool logging = false;
        private float logTimer = 0f;
        private const float LOG_INTERVAL = 0.1f;
        
        public override void Configure(IModConfig config) { }
        
        public void Start()
        {
            ModHelper.Console.WriteLine("ShipLogger LOADED - Press P to toggle", MessageType.Success);
        }
        
        private void InitializeLogging()
        {
            using (var csv = File.CreateText("ship_log.csv"))
            {
                csv.WriteLine("time,shipX,shipY,shipZ,shipVelX,shipVelY,shipVelZ," +
                             "shipRotX,shipRotY,shipRotZ,shipRotW");
            }
            ModHelper.Console.WriteLine("=== LOGGING INITIALIZED - SHIP + ORIENTATION ===", MessageType.Message);
        }
        
        private void LogShipData()
        {
            var shipController = Locator.GetShipBody();
            if (shipController == null) return;
            
            Transform shipTransform = shipController.transform;
            OWRigidbody owRb = shipController.GetComponent<OWRigidbody>();
            Vector3 shipPos = shipTransform.position;
            Vector3 shipVel = owRb != null ? owRb.GetVelocity() : Vector3.zero;
            Quaternion shipRot = shipTransform.rotation;
            
            string line = string.Format("{0:F3},{1:F2},{2:F2},{3:F2},{4:F2},{5:F2},{6:F2}," +
                                       "{7:F3},{8:F3},{9:F3},{10:F3}",
                Time.time, shipPos.x, shipPos.y, shipPos.z, shipVel.x, shipVel.y, shipVel.z,
                shipRot.x, shipRot.y, shipRot.z, shipRot.w);
            
            using (var csv = File.AppendText("ship_log.csv"))
            {
                csv.WriteLine(line);
            }
            
            ModHelper.Console.WriteLine(string.Format("[SHIP] POS:{0,6:F0} SPD:{1:F1} ROT:{2:F1}",
                shipPos.magnitude, shipVel.magnitude, shipRot.eulerAngles.y), MessageType.Message);
        }
        
        public void Update()
        {
            // Press O = Speed up time x10
            if (Keyboard.current.oKey.wasPressedThisFrame)
            {
                Time.timeScale = Time.timeScale == 1f ? 10f : 1f;
                ModHelper.Console.WriteLine("Time scale: " + Time.timeScale, MessageType.Success);
            }


            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                logging = !logging;
                if (logging)
                {
                    InitializeLogging();
                    logTimer = 0f;
                }
                else
                {
                    ModHelper.Console.WriteLine("=== LOGGING STOPPED ===", MessageType.Warning);
                }
                ModHelper.Console.WriteLine("Logging: " + (logging ? "ON" : "OFF"), MessageType.Success);
            }
            
            if (logging)
            {
                logTimer += Time.deltaTime;
                if (logTimer >= LOG_INTERVAL)
                {
                    LogShipData();
                    logTimer = 0f;
                }
            }
        }
    }
}


//---------------------------------------------------
// using System;
// using OWML.Common;
// using OWML.ModHelper;
// using UnityEngine;
// using UnityEngine.InputSystem;
// using System.IO;
// using System.Linq;

// namespace ShipLogger
// {
//     public class ShipLogger : ModBehaviour
//     {
//         private bool logging = false;
        
//         public override void Configure(IModConfig config) { }
        
//         public void Start()
//         {
//             ModHelper.Console.WriteLine("ShipLogger LOADED - Press P to toggle", MessageType.Success);
//         }
        
//         public void Update()
//         {
//             if (Keyboard.current.pKey.wasPressedThisFrame)
//             {
//                 logging = !logging;
//                 if (logging)
//                 {
//                     using (var csv = File.CreateText("ship_log.csv"))
//                     {
//                         csv.WriteLine("time,posX,posY,posZ,velX,velY,velZ");
//                     }
//                 }
//                 ModHelper.Console.WriteLine("Logging: " + (logging ? "ON" : "OFF"));
//             }
            
//             if (logging)
//             {
//                 var shipController = Locator.GetShipBody();
//                 ModHelper.Console.WriteLine("Ship Controller: " + (shipController != null ? "Found" : "Not Found"));
                
//                 if (shipController != null)
//                 {
//                     Transform shipTransform = shipController.transform;
//                     OWRigidbody owRb = shipController.GetComponent<OWRigidbody>();
//                     Vector3 pos = shipTransform.position;
//                     Vector3 vel = owRb != null ? owRb.GetVelocity() : Vector3.zero;
                    
//                     string line = string.Format("{0:F3},{1:F2},{2:F2},{3:F2},{4:F2},{5:F2},{6:F2}", 
//                         Time.time, pos.x, pos.y, pos.z, vel.x, vel.y, vel.z);
                    
//                     using (var csv = File.AppendText("ship_log.csv"))
//                     {
//                         csv.WriteLine(line);
//                     }
                    
//                     ModHelper.Console.WriteLine("POS: " + 
//                         Mathf.RoundToInt(pos.x) + "," + 
//                         Mathf.RoundToInt(pos.y) + "," + 
//                         Mathf.RoundToInt(pos.z) + 
//                         " SPD: " + vel.magnitude.ToString("F1"));
//                 }
//             }
//         }
//     }
// }-------------------------------above works 

// using System;
// using OWML.Common;
// using OWML.ModHelper;
// using UnityEngine;
// using UnityEngine.InputSystem;
// using System.IO;

// namespace ShipLogger
// {
//     public class ShipLogger : ModBehaviour
//     {
//         private StreamWriter log;
//         private bool logging = false;
        
//         public override void Configure(IModConfig config) { }
        
//         public void Start()
//         {
//             ModHelper.Console.WriteLine("ShipLogger LOADED - Press P to toggle", MessageType.Success);
//         }
        
//         public void Update()
//         {

//             ModHelper.Console.WriteLine("ULogging: " + (logging ? "ON" : "OFF"), MessageType.Debug);
            
//             // CHANGED: P key instead of TAB
//             if (Keyboard.current.pKey.wasPressedThisFrame)
//             {
//                 logging = !logging;
//                 if (logging)
//                 {
//                     log = new StreamWriter("ship_log.csv", false);
//                     log.WriteLine("time,posX,posY,posZ,velX,velY,velZ");
//                     log.Close();
//                 }
//                 ModHelper.Console.WriteLine("Logging: " + (logging ? "ON" : "OFF"), MessageType.Debug);
//             }
            
//             if (logging)
//             {
//                 var shipObj = GameObject.Find("Ship");
//                 ModHelper.Console.WriteLine("Ship Object: " + (shipObj != null ? "Found" : "Not Found"), MessageType.Debug);
//                 if (shipObj != null)
//                 {
//                     Transform shipTransform = shipObj.transform;
//                     Vector3 pos = shipTransform.position;
//                     Rigidbody rb = shipTransform.GetComponent<Rigidbody>();
//                     Vector3 vel = rb != null ? rb.velocity : Vector3.zero;
                    
//                     string line = $"{Time.time:F3},{pos.x:F2},{pos.y:F2},{pos.z:F2}," +
//                                  $"{vel.x:F2},{vel.y:F2},{vel.z:F2}";
                    
//                     using (var csv = File.AppendText("ship_log.csv"))
//                     {
//                         csv.WriteLine(line);
//                     }
                    
//                     ModHelper.Console.WriteLine("POS: " + 
//                         Mathf.RoundToInt(pos.x) + "," + 
//                         Mathf.RoundToInt(pos.y) + "," + 
//                         Mathf.RoundToInt(pos.z) + 
//                         " SPD: " + vel.magnitude.ToString("F1"));
//                 }
//             }
//         }
//     }
// }
