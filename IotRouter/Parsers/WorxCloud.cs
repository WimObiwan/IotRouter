using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IotRouter.Parsers;

public class WorxCloud : Parser
{
    public WorxCloud(IServiceProvider serviceProvider, IConfigurationSection config, string name)
        : base(serviceProvider.GetService<ILogger<WorxCloud>>(), name)
    {
    }

    public override ParsedData Parse(byte[] data)
    {
        _logger.LogInformation("Data: {Data}", Encoding.UTF8.GetString(data));
        
        var packet = JsonSerializer.Deserialize<Packet>(data);
        
        string devEUI = packet.dat.uuid; // parserData.GetDevEUI();
        DateTime dateTime = packet.dat.tm; //parserData.GetTime();

        var tomorrowDay = DateTime.Now.AddDays(1).Date.DayOfWeek;
        var slot = packet.cfg.sc.slots.FirstOrDefault(s => s.d == (int)tomorrowDay);
        TimeSpan tomorrowStartTime;
        if (slot != null)
            tomorrowStartTime = TimeSpan.FromMinutes(slot.s);
        else
            tomorrowStartTime = TimeSpan.Zero;

        var keyValues = new List<ParsedData.KeyValue>()
        {
            new("TomorrowStartTime", (int)tomorrowStartTime.TotalSeconds),
            new("Status", (int)packet.dat.ls),
            new("StatusText", packet.dat.ls.ToString()),
            new("Error", (int)packet.dat.le),
            new("ErrorText", packet.dat.le.ToString()),
            new("battery_level", packet.dat.bt.p),
            new("BatteryChargeCount", packet.dat.bt.nr),
            new("BatteryChargeStatus", (int)packet.dat.bt.c),
            new("BatteryChargeStatusText", packet.dat.bt.c.ToString()),
            new("Zone", packet.dat.cut.z),
            new("latitude", packet.dat.modules._4g.gps.coo[0]),
            new("longitude", packet.dat.modules._4g.gps.coo[1]),
            new("RainStatus", packet.dat.rain.s),
            new("RainRemaining", packet.dat.rain.cnt),
            new("Firmware", packet.dat.fw),
            new("HeadFirmware", packet.dat.head.fw)
        };

        return new ParsedData(devEUI, dateTime, keyValues);
    }

    private class Packet
    {
        public class Config
        {
            public class Sc
            {
                public class Slot
                {
                    public int d { get; init; }
                    public int s { get; init; }
                    public int t { get; init; }
                }
                
                public Slot[] slots { get; init; }
            }

            public Sc sc { get; init; }
    }
        
        public class Data
        {
            public enum Status
            {
                Idle = 0,
                Home = 1,
                StartSequence = 2,
                LeavingHome = 3,
                FollowWire = 4,
                SearchingHome = 5,
                SearchingWire = 6,
                Mowing = 7,
                Lifted = 8,
                Trapped = 9,
                BladeBlocked = 10,
                Debug = 11,
                RemoteControl = 12,
                GoingHome = 30,
                Zoning = 31,
                CuttingEdge = 32,
                SearchingArea = 33,
                Pause = 34,
                __UpgradingFirmware__ = 102,
                __Correcting__ = 103,
                __GoingHome__ = 104
            }

            public enum Error
            {
                NoError = 0,
                Trapped = 1,
                Lifted = 2,
                WireMissing = 3,
                OutsideWire = 4,
                RainDelay = 5,
                CloseDoorToMow = 6,
                CloseDoorToGoHome = 7,
                BladeMotorBlocked = 8,
                WheelMotorBlocked = 9,
                TrappedTimeout = 10,
                UpsideDown = 11,
                BatteryLow = 12,
                ReverseWire = 13,
                ChargeError = 14,
                TimeoutFindingHome = 15,
                Locked = 16,
                BatteryTemperatureError = 17,
                DummyModel = 18,
                BatteryTrunkOpenTimeout = 19,
                WireSync = 20,
                MsgNum = 21,
                __ErrorGoingHome__ = 100,
                __NoPosition__ = 108,
            }

            public class Battery
            {
                public enum Charging
                {
                    NotCharging = 0,
                    Charging = 1,
                    Error = 2
                }

                public int p { get; init; }
                public int nr { get; init; }
                public Charging c { get; init; }
            }

            public class Cut
            {
                public int z { get; init; }
            }

            public class Modules
            {
                public class _4G
                {
                    public class Gps
                    {
                        public double[] coo { get; init; }
                    }

                    public Gps gps { get; init; }
                }

                [JsonPropertyName("4G")]
                public _4G _4g { get; init; }
            }

            public class Rain
            {
                public int s { get; init; }
                public int cnt { get; init; }
            }

            public class Head
            {
                public string fw { get; init; }
            }

            public string uuid { get; init; }
            public DateTime tm { get; init; }
            public Status ls { get; init; }
            public Error le { get; init; }
            public Battery bt { get; init; }
            public Cut cut { get; init; }
            public Modules modules { get; init; }
            public Rain rain { get; init; }
            public string fw { get; init; }
            public Head head { get; init; }
        }

        public Config cfg { get; init; }
        public Data dat { get; init; }
    }
}

