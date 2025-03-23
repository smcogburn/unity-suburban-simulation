using System;

namespace UrbanSim
{
    [Flags]
    public enum TransportMode
    {
        None = 0,
        Walking = 1 << 0,
        Driving = 1 << 1,
        // Future expansion
        Cycling = 1 << 2,
        PublicTransport = 1 << 3
    }
}