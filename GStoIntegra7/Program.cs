using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace GStoIntegra7
{
    class MidiProgramChange
    {
        public byte? bankLSB;
        public byte? bankMSB;
        public byte? PCNum;

        public MidiProgramChange(byte? bankMSB, byte? bankLSB, byte? pCNum)
        {
            this.bankLSB = bankLSB;
            this.bankMSB = bankMSB;
            PCNum = pCNum;
        }

        public MidiProgramChange()
        {
            bankLSB = 0;
            bankMSB = 0;
            PCNum = 0;
        }
    }
    class PCevent
    {
       public byte chan;
       public MidiProgramChange Program = new MidiProgramChange();

        public PCevent()
        {
            chan = 0;
            Program.bankMSB = 0;
            Program.bankLSB = 0;
            Program.PCNum = 0;
        }
        public override string ToString()
        {
            return $"channel:{chan}, LSB:{Program.bankLSB}, MSB:{Program.bankMSB}, Program:{Program.PCNum}";
        }

        /// <summary>
        /// null = always true
        /// </summary>
        /// <param name="p">mpc</param>
        /// <returns></returns>
        public bool Equals(MidiProgramChange p)
        {
             if (
                (p.bankMSB == null ? true : (p.bankMSB == this.Program.bankMSB)) &&
                 (p.bankLSB == null ? true : (p.bankLSB == this.Program.bankLSB)) &&
                 (p.PCNum == null ? true : (p.PCNum == this.Program.PCNum))
             )
             {
                 return true;
             }
            return false;
        }
    }
    internal class Program
    {
        static InputDevice inputDeviceA = null;
        static  OutputDevice outputDeviceA = null;
        static PCevent[] PC = new PCevent[16];

        static void Main(string[] args)
        {
            var exitEvent = new ManualResetEvent(false);
            for (int i=0; i<PC.Length; i++)
            {
                PC[i] = new PCevent();
            }
            {
                Console.WriteLine("Select input midi device:");
                for (int i = 0; i < InputDevice.GetDevicesCount(); i++)
                {
                    InputDevice midid = InputDevice.GetByIndex(i);
                    Console.WriteLine("[" + i + "]" + midid.Name);
                }
                Console.Write("> ");
                string r = Console.ReadLine();
                inputDeviceA = InputDevice.GetByIndex(int.Parse(r));
                inputDeviceA.EventReceived += OnEventReceivedA;
                Console.WriteLine(inputDeviceA.Name + " selected.");
                
            }
            {
                Console.WriteLine("Select output midi device:");

                for (int i = 0; i < OutputDevice.GetDevicesCount(); i++)
                {
                    OutputDevice midid = OutputDevice.GetByIndex(i);
                    Console.WriteLine("[" + i + "]" + midid.Name);
                }
                Console.Write("> ");
                string r = Console.ReadLine();
                outputDeviceA = OutputDevice.GetByIndex(int.Parse(r));
                Console.WriteLine(outputDeviceA.Name + " selected.");

            }
            inputDeviceA.StartEventsListening();
            Console.WriteLine("Listening...");
            

            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            exitEvent.WaitOne();

            inputDeviceA.StopEventsListening();
            inputDeviceA.Dispose();
            outputDeviceA.TurnAllNotesOff();
            outputDeviceA.Dispose();
            Console.WriteLine("Exit");
            
            //Console.ReadLine();
        }
        private static void OnEventReceivedA(object sender, MidiEventReceivedEventArgs e)
        {
            //Console.WriteLine(e.Event.EventType);
            //MidiEventToBytesConverter x = new MidiEventToBytesConverter();
            //ChannelEvent ev = ((ChannelEvent)e.Event);

            //Console.WriteLine(data.Length);
            if (e.Event.EventType == MidiEventType.ControlChange)
            {
                //byte[] data = x.Convert(e.Event);
                ControlChangeEvent controlChangeEvent = (ControlChangeEvent)e.Event;
                
                if (controlChangeEvent.ControlNumber == 0x20) //LSB
                {
                    PC[controlChangeEvent.Channel].Program.bankLSB = controlChangeEvent.ControlValue;
                    //PC.bankLSB = data[2];
                    //Console.WriteLine("{0} {1} {2}", data[0], data[1], data[2]);
                }
                else if (controlChangeEvent.ControlNumber == 0) //MSB
                {
                    PC[controlChangeEvent.Channel].Program.bankMSB = controlChangeEvent.ControlValue;
                    //Console.WriteLine("{0} {1} {2}", data[0], data[1], data[2]);
                }
                else
                {
                    outputDeviceA.SendEvent(controlChangeEvent);
                }

                
            }
            else if(e.Event.EventType == MidiEventType.ProgramChange)
            {
                ProgramChangeEvent pce = (ProgramChangeEvent)e.Event;
                PC[pce.Channel].Program.PCNum = pce.ProgramNumber;
                PC[pce.Channel].chan = pce.Channel;
                Console.WriteLine(PC[pce.Channel].ToString());

                if(pce.Channel == 9)
                {
                    if (PC[pce.Channel].Equals(new MidiProgramChange(0, null, 0)))
                    {
                        sendPCEvent(pce.Channel, 88, 64, 5);
                        Console.WriteLine("Cn:{1} PC Change {0} to 88, 64, 5", PC[pce.Channel].ToString(),pce.Channel);
                        return;
                    }
                    else
                    {
                        sendPCEvent(pce.Channel, 88, 64, 0);
                        return;
                    }
                }
                else if (pce.Channel == 10)
                {
                    if (PC[pce.Channel].Equals(new MidiProgramChange(0, null, 0)))
                    {
                        sendPCEvent(pce.Channel, 88, 64, 5);
                        Console.WriteLine("Cn:{1} PC Change {0} to 88, 64, 5", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else
                    {
                        sendPCEvent(pce.Channel, 88, 64, 0);
                        return;
                    }
                }
                else
                {
                    if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 0))) //Piano 1
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 64, 0);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 64, 1", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 1))) //Piano 2
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 64, 4);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 64, 5", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 1))) //Piano 3
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 64, 3);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 64, 5", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 18))) //Organ 3
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 64, 58);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 64, 58", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 22))) //Harmonica
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 65, 34);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 65, 34", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 25))) //Steel
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 65, 29);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 65, 29", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 27))) //Clean
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 64, 90);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 64, 90", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 30))) //Dist GT
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 65, 1);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 65, 1", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 32))) //Acoustic Bass
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 65, 6);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 65, 7", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 33))) //FingBass
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 65, 8);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 65, 9", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 34))) //Pickbass
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 65, 14);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 65, 15", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 35))) //Flatless
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 65, 18);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 65, 19", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 36))) //Slap1 
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 65, 13);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 65, 19", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }

                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 37))) //Slap2 
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 65, 13);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 65, 19", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }

                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 48))) //String1
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 65, 74);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 65, 74", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 49))) //String1
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 65, 75);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 65, 75", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 64))) //Sop sax
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 65, 102);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 65, 102", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                    else if (PC[pce.Channel].Equals(new MidiProgramChange(null, null, 71))) //Clarinet
                    {
                        Console.WriteLine("Non-Drum");
                        sendPCEvent(pce.Channel, 89, 65, 114);
                        Console.WriteLine("Cn:{1} PC Change {0} to 89, 65, 114", PC[pce.Channel].ToString(), pce.Channel);
                        return;
                    }
                }

                outputDeviceA.SendEvent(pce);
                
            }
            else if (e.Event.EventType == MidiEventType.NoteOn)
            {
                NoteOnEvent noteOn = (NoteOnEvent)e.Event;
                if(noteOn.Channel == 9 || noteOn.Channel == 10)
                {
                    if(noteOn.NoteNumber == 0x39 || noteOn.NoteNumber == 0x31)
                    {
                        short velOrig = noteOn.Velocity;
                        short vel = (short)(velOrig + 30);
                        if (vel > 0x7f) vel = 0x7f;
                        noteOn.Velocity = (SevenBitNumber)vel;
                        Console.WriteLine("drum note vel change: {0} -> {1}, note: {2}", velOrig,vel,noteOn.NoteNumber);
                    }
                    
                }
                outputDeviceA.SendEvent(noteOn);
            }
            else
            {
                outputDeviceA.SendEvent(e.Event);
            }
            
        }

        static void sendPCEvent(byte ChanNum, byte MSB, byte LSB, byte PC)
        {
            //MSB
            ControlChangeEvent CC = new ControlChangeEvent();
            CC.Channel = (FourBitNumber)ChanNum;
            CC.ControlValue = (SevenBitNumber)MSB;
            CC.ControlNumber = (SevenBitNumber)0;
            outputDeviceA.SendEvent(CC);

            //LSB
            CC.Channel = (FourBitNumber)ChanNum;
            CC.ControlValue = (SevenBitNumber)LSB;
            CC.ControlNumber = (SevenBitNumber)0x20;
            outputDeviceA.SendEvent(CC);

            //PC
            ProgramChangeEvent programChangeEvent = new ProgramChangeEvent();
            programChangeEvent.Channel = (FourBitNumber)ChanNum;
            programChangeEvent.ProgramNumber = (SevenBitNumber)PC;
            outputDeviceA.SendEvent(programChangeEvent);
        }

       
        private static void ConvertPC(PCevent p)
        {
            //if (p.Equals())
            {

            }
        }
    }



    static class GSProgram
    {
        static MidiProgramChange Piano0_1 = new MidiProgramChange(0,0,0);
        static MidiProgramChange Piano0_2 = new MidiProgramChange(0,0,0);
        static MidiProgramChange DrumStd = new MidiProgramChange(0, 0, 0);
    }
}
