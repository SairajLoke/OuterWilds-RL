// PSIPIDController.cs
using OWML.Common;
using UnityEngine;

namespace OuterWildsModPsi
{
    public class PSIPIDController
    {
        private ShipBody shipBody = null;
        private OWRigidbody hostPlanet = null;
        private ShipAltimeter altimeter;
        private ForceDetector forceDetector;
        private Vector3 shipPosition;
        private Quaternion shipRotation;
        private Vector3 shipAcceleration;
        private Vector3 shipVelocity;

        public bool foundShip = false;

        // FIX 4: injected console reference
        private IModHelper modHelper;

        public PSIPIDController(IModHelper modHelper)
        {
            this.modHelper = modHelper;
        }

        public void getMyShip()
        {
            this.shipBody = (ShipBody)Locator.GetShipBody();

            if (this.shipBody != null)
            {
                this.altimeter = shipBody.GetComponent<ShipAltimeter>();
                this.forceDetector = shipBody.GetComponent<ForceDetector>();

                // FIX 5: set foundShip true
                this.foundShip = true;

                modHelper.Console.WriteLine(
                    $"Ship found! Altimeter: {altimeter != null} | " +
                    $"Force: {forceDetector != null}",
                    MessageType.Success);
            }
            else
            {
                modHelper.Console.WriteLine(
                    "Ship not found", MessageType.Error);
            }
        }

        public void LogShipData()
        {
            if (this.shipBody == null)
            {
                modHelper.Console.WriteLine("Ship Body null", MessageType.Error);
                foundShip = false; // FIX 6: reset so Update searches again
                return;
            }

            this.shipPosition = shipBody.GetPosition();
            this.shipRotation = shipBody.GetRotation();
            this.shipAcceleration = shipBody.GetAcceleration();
            this.shipVelocity = shipBody.GetVelocity();

            // FIX 7: null check before using forceDetector
            Vector3 forceAccel = forceDetector != null
                ? forceDetector.GetForceAcceleration()
                : Vector3.zero;

            modHelper.Console.WriteLine(
                string.Format("[SHIP] POS:{0,6:F0} SPD:{1:F1} ROT:{2:F1} FORCE:{3:F2}",
                    shipPosition.magnitude,
                    shipVelocity.magnitude,
                    shipRotation.eulerAngles.y,
                    forceAccel.magnitude),
                MessageType.Success);
        }
    }
}