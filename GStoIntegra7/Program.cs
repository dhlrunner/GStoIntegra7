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
using System.IO;

namespace GStoIntegra7
{
    class PCMap
    {
        public byte? SbankMSB;
        public byte? SbankLSB;
        public byte? SProgramNum;
        public byte DbankMSB;
        public byte DbankLSB;
        public byte DProgramNum;

        public PCMap(byte? sbankMSB, byte? sbankLSB, byte? sProgramNum, byte dbankMSB, byte dbankLSB, byte dProgramNum)
        {
            SbankMSB = sbankMSB;
            SbankLSB = sbankLSB;
            SProgramNum = sProgramNum;
            DbankMSB = dbankMSB;
            DbankLSB = dbankLSB;
            DProgramNum = dProgramNum;
        }
    }
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
        /// Program Change이벤트 비교
        /// MSB,LSB,Program ID비교
        /// null인 경우는 비교 대상에 추가하지 않음
        /// </summary>
        /// <param name="p"></param>
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
        static List<PCMap> non_drum_map = new List<PCMap>();
        static List<PCMap> drum_map = new List<PCMap>();

        static void Main(string[] args)
        {
            var exitEvent = new ManualResetEvent(false);
            for (int i=0; i<PC.Length; i++)
            {
                PC[i] = new PCevent();
            }

            try
            {
                parsePCMapFromFile(non_drum_map, "./nondrum.map");
                parsePCMapFromFile(drum_map, "./drum.map");

                Console.WriteLine("Midi Program Change Map Parsed. non-drum:{0}, drum:{1}", non_drum_map.Count, drum_map.Count);
            }
            catch (Exception ex)
            {
                
                Console.WriteLine(ex.Message);
                return;
            }

            //input 장치 없을 경우 종료
            if (InputDevice.GetDevicesCount() <= 0)
            {
                Console.WriteLine("No input devices found");
                return;
            }

            //input 장치 선택
            {
                int sel = -1;
                while (sel < 0)
                {
                    Console.WriteLine("Select input midi device:");
                    for (int i = 0; i < InputDevice.GetDevicesCount(); i++)
                    {
                        InputDevice midid = InputDevice.GetByIndex(i);
                        Console.WriteLine("[" + i + "]" + midid.Name);
                    }
                    Console.Write("> ");
                    string r = Console.ReadLine();
                    if (int.TryParse(r, out sel))
                    {
                        if (sel < InputDevice.GetDevicesCount())
                        {
                            inputDeviceA = InputDevice.GetByIndex(sel);
                        }
                        else
                        {
                            sel = -1;
                            Console.WriteLine("Invalid selection.");
                        }
                        
                    }
                    else
                    {
                        sel = -1;
                        Console.WriteLine("Invalid selection.");
                    }
                }
                Console.WriteLine(inputDeviceA.Name + " selected.");

            }
            
            //output 장치 선택
            {
                int sel = -1;
                while (sel < 0)
                {
                    Console.WriteLine("Select output midi device:");
                    for (int i = 0; i < OutputDevice.GetDevicesCount(); i++)
                    {
                        OutputDevice midid = OutputDevice.GetByIndex(i);
                        Console.WriteLine("[" + i + "]" + midid.Name);
                    }
                    Console.Write("> ");
                    string r = Console.ReadLine();
                    if (int.TryParse(r, out sel))
                    {
                        if (sel < OutputDevice.GetDevicesCount())
                        {
                            outputDeviceA = OutputDevice.GetByIndex(sel);
                        }
                        else
                        {
                            sel = -1;
                            Console.WriteLine("Invalid selection.");
                        }
                    }
                    else
                    {
                        sel = -1;
                        Console.WriteLine("Invalid selection.");
                    }
                }                                        
                Console.WriteLine(outputDeviceA.Name + " selected.");

            }
            Console.WriteLine("Input device: " + inputDeviceA.Name);
            Console.WriteLine("Output device: " + outputDeviceA.Name);
            inputDeviceA.EventReceived += OnEventReceivedA;
            inputDeviceA.StartEventsListening();

            //Expansion 셋 전송
            Console.WriteLine("Sending Expansion set parameter..");
            outputDeviceA.SendEvent(new NormalSysExEvent(new byte[] { 0x41, 0x10, 0x00, 0x00, 0x64, 0x11, 0x0F, 0x00, 0x30, 0x00, 0x05, 0x0D, 0x08, 0x12, 0x15, 0xF7 }));

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
            if (e.Event.EventType == MidiEventType.ControlChange)
            {
                ControlChangeEvent controlChangeEvent = (ControlChangeEvent)e.Event;           
                if (controlChangeEvent.ControlNumber == 0x20) //LSB
                {
                    PC[controlChangeEvent.Channel].Program.bankLSB = controlChangeEvent.ControlValue;
                }
                else if (controlChangeEvent.ControlNumber == 0) //MSB
                {
                    PC[controlChangeEvent.Channel].Program.bankMSB = controlChangeEvent.ControlValue;
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
                
                //TODO: SysEx로 드럼 채널 판정             
                if (pce.Channel == 9 ||pce.Channel == 10)
                {
                    foreach (PCMap map in drum_map)
                    {
                        if (PC[pce.Channel].Equals(new MidiProgramChange(map.SbankMSB, map.SbankLSB, map.SProgramNum)))
                        {
                            sendPCEvent(pce.Channel, map.DbankMSB, map.DbankLSB, map.DProgramNum);
                            Console.WriteLine("Cn:{1} PC Change {0} to {2},{3},{4}", PC[pce.Channel].ToString(), pce.Channel, map.DbankMSB, map.DbankLSB, map.DProgramNum);
                            return;
                        }
                    }          
                }
                else
                {
                    //Program Change 변환 부분
                    foreach(PCMap map in non_drum_map)
                    {
                        if (PC[pce.Channel].Equals(new MidiProgramChange(map.SbankMSB,map.SbankLSB,map.SProgramNum))) 
                        {                          
                            sendPCEvent(pce.Channel, map.DbankMSB,map.DbankLSB,map.DProgramNum);
                            Console.WriteLine("Cn:{1} PC Change {0} to {2},{3},{4}", PC[pce.Channel].ToString(), pce.Channel, map.DbankMSB, map.DbankLSB, map.DProgramNum);
                            
                            if (PC[pce.Channel].Equals(new MidiProgramChange(8, null, 80))) //Sin wave
                            {
                                //기본 프리셋이 Mono 모드이기 때문에 CC 127 신호로 Poly모드로 바꿔준다
                                ControlChangeEvent c = new ControlChangeEvent();
                                c.ControlNumber = (SevenBitNumber)0x7F;
                                c.ControlValue = (SevenBitNumber)0x7F;
                                Console.WriteLine("Set to Poly mode");
                                outputDeviceA.SendEvent(c);
                            }
                            
                            return;
                        }
                    }
                    
                    
                    
                }

                outputDeviceA.SendEvent(pce);
                
            }
            else if (e.Event.EventType == MidiEventType.NoteOn)
            {
                NoteOnEvent noteOn = (NoteOnEvent)e.Event;
                if(noteOn.Channel == 9 || noteOn.Channel == 10) //TODO: SysEx로 드럼 채널 판정
                {
                    
                    if (noteOn.Velocity != 0) //Note OFF가 Note On신호의 벨로시티 0인 경우도 있기 때문
                    {
                        //Crash 심벌 벨로시티가 낮으면 SuperNatural 드럼에서는 너무 작게 들리기 때문에 이를 조정해준다. (약 +40 정도가 적당)
                        if (noteOn.NoteNumber == 0x39 || noteOn.NoteNumber == 0x31)
                        {
                            short velOrig = noteOn.Velocity;
                            short vel = (short)(velOrig + 40);
                            if (vel > 0x7f) vel = 0x7f;
                            noteOn.Velocity = (SevenBitNumber)vel;
                            Console.WriteLine("drum note vel change: {0} -> {1}, note: {2}", velOrig, vel, noteOn.NoteNumber);
                        }
                        else if(noteOn.NoteNumber == 0x28) //스네어 소리 조절
                        {
                            short velOrig = noteOn.Velocity;
                            short vel = (short)(velOrig - 20);
                            if (vel < 1) vel = 1;
                            noteOn.Velocity = (SevenBitNumber)vel;
                            Console.WriteLine("drum note vel change: {0} -> {1}, note: {2}", velOrig, vel, noteOn.NoteNumber);
                        }

                    }
                    
                    
                }
                else 
                {
                    if(PC[noteOn.Channel].Equals(new MidiProgramChange(6, null, 120)))
                    {
                        byte note = (byte)noteOn.NoteNumber;
                        byte Cnote = (byte)noteOn.NoteNumber;
                        if(Cnote > 0x18) Cnote = 0x18;
                        noteOn.NoteNumber = (SevenBitNumber)Cnote;
                        Console.WriteLine("Pick Scrape note change {0} -> {1}",note,Cnote);
                    }
                    
                }
                outputDeviceA.SendEvent(noteOn);
            }
            else if(e.Event.EventType == MidiEventType.NormalSysEx)
            {
                NormalSysExEvent nse = (NormalSysExEvent)e.Event;
                //print sysex hex string from NormalSysExEvent
                string hexString = "";
                foreach (byte b in nse.Data)
                {
                    hexString += b.ToString("X2") + " ";
                }
                Console.WriteLine("SysEx: {0}", hexString);
                outputDeviceA.SendEvent(e.Event);
            }
            else
            {
                outputDeviceA.SendEvent(e.Event);
            }
            
        }

        static void parsePCMapFromFile(List<PCMap> pc, string filename)
        {
            if (File.Exists(filename))
            {
                pc.Clear();
                string[] lines = File.ReadAllLines(filename);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    
                    if (line.Contains("//")) //주석
                    {
                        //split string by
                        line = line.Split(new string[] { "//" }, StringSplitOptions.None)[0];
                    }

                    line = line.Trim();
                    
                    if (line.Length > 0)
                    {
                        string[] split = line.Split(':');
                        if(split.Length > 1)
                        {
                            string[] source = split[0].Split(',');
                            string[] dest = split[1].Split(',');

                            if (source.Length > 2 && dest.Length > 2)
                            {
                                byte? sM = source[0].ToLower() == "null" ? (byte?)null : byte.Parse(source[0]);
                                byte? sL = source[1].ToLower() == "null" ? (byte?)null : byte.Parse(source[1]);
                                byte? sPC = source[2].ToLower() == "null" ? (byte?)null : (byte)(byte.Parse(source[2]) - 1);

                                byte dM =  byte.Parse(dest[0]);
                                byte dL = byte.Parse(dest[1]);
                                byte dPC = (byte)(byte.Parse(dest[2]) - 1);

                                pc.Add(new PCMap(sM, sL, sPC, dM, dL, dPC));
                            }
                            else
                            {
                                throw new Exception($"PCMap file format error at Line {i+1}");
                            }
                        }
                        else { throw new Exception($"PCMap file format error at Line {i + 1}"); }
                    }
                    
                }
            }
            else
            {
                throw new FileNotFoundException($"{filename} not found.");
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
}
