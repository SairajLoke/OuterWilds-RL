using UnityEngine;

namespace OuterWildsModPsi
{
    /// <summary>
    /// All parameters needed to define and execute an orbit.
    /// Created by OrbitConfigMenu, consumed by PSIPIDController.
    /// </summary>
    public struct OrbitParameters
    {
        /// <summary>Target altitude above planet surface in metres.</summary>
        public float altitude;

        /// <summary>
        /// Desired orbital speed in m/s.
        /// If userOverrideSpeed is false this was auto-calculated for a circular orbit.
        /// </summary>
        public float speed;

        /// <summary>True when the user manually typed a speed value.</summary>
        public bool userOverrideSpeed;

        /// <summary>
        /// Orbit plane tilt in degrees.
        /// 0   = equatorial (orbit around equator)
        /// 90  = polar      (orbit over poles)
        /// Values between give an inclined orbit.
        /// </summary>
        public float axisAngle;

        /// <summary>True = prograde (same direction as planet rotation), False = retrograde.</summary>
        public bool prograde;

        /// <summary>The OWRigidbody of the planet being orbited.</summary>
        public OWRigidbody targetBody;

        /// <summary>
        /// Calculates the theoretical circular-orbit speed at the requested altitude
        /// using the planet's surface gravity and radius.
        /// Returns 0 if the planet has no GravityVolume.
        ///// </summary>
        //public static float CalculateCircularOrbitSpeed(OWRigidbody body, float altitude)
        //{
        //    if (body == null) return 0f;

        //    GravityVolume gv = body.GetAttachedGravityVolume();
        //    if (gv == null) return 0f;

        //    // v_circular = sqrt(g_surface * R² / r)
        //    // where r = R + altitude
        //    float surfaceAccel  = gv.GetSurfaceAcceleration();
        //    float surfaceRadius = gv.GetSphereOfInfluence(); // closest usable proxy for surface radius

        //    // Better: use the gravity volume's actual surface radius field via reflection if needed,
        //    // but _upperSurfaceRadius is a reasonable stand-in for most spherical bodies.
        //    float orbitRadius = surfaceRadius + altitude;
        //    if (orbitRadius <= 0f) return 0f;

        //    return Mathf.Sqrt(surfaceAccel * surfaceRadius * surfaceRadius / orbitRadius);
        //}
        /// <summary>
        /// Calculates circular orbit speed at a given altitude above the planet surface.
        /// Uses ship's current position as the surface reference — call this when ship
        /// has just arrived at the planet (autopilot complete), so the distance is accurate.
        /// </summary>
        public static float CalculateCircularOrbitSpeed(OWRigidbody planetBody,
                                                         Vector3 shipWorldPosition,
                                                         float extraAltitude)
        {
            if (planetBody == null) return 0f;

            GravityVolume gv = planetBody.GetAttachedGravityVolume();
            if (gv == null) return 0f;

            // GM = standard gravitational parameter, game already computes this
            float GM = gv.GetStandardGravitationalParameter();

            // r = current distance ship→planet + any extra altitude requested
            // This works correctly: ship just arrived, so current dist ≈ surface dist
            float r = (shipWorldPosition - planetBody.GetPosition()).magnitude + extraAltitude;

            if (r <= 0f) return 0f;
            return Mathf.Sqrt(GM / r);
        }

        public override string ToString()
        {
            return string.Format(
                "Target={0} Alt={1:F0}m Speed={2:F1}m/s({3}) Axis={4:F0}° {5}",
                targetBody != null ? targetBody.name : "null",
                altitude,
                speed,
                userOverrideSpeed ? "manual" : "auto",
                axisAngle,
                prograde ? "Prograde" : "Retrograde");
        }
    }
}
