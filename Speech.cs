//------------------------------------------------------------------------------
// <copyright file="SpeechRecognizer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

// This module provides sample code used to demonstrate the use
// of the KinectAudioSource for speech recognition in a game setting.

// IMPORTANT: This sample requires the Speech Platform SDK (v11) to be installed on the developer workstation

namespace Moto.Speech
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.Kinect;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using System.Windows.Media.Imaging;
    using System.Windows.Controls;
    using System.Windows.Threading;

    public class SpeechRecognizer : IDisposable
    {
        private readonly Dictionary<string, WhatSaid> startListeningPhrases = new Dictionary<string, WhatSaid>
            {
                { "Kinect", new WhatSaid { Verb = Verbs.SpeechStart} },
                { "Moto", new WhatSaid { Verb = Verbs.SpeechStart} },
                { "Mowtow", new WhatSaid { Verb = Verbs.SpeechStart} },
            };

        private readonly Dictionary<string, WhatSaid> voicePromptPhrases = new Dictionary<string, WhatSaid>
            {
                { " ", new WhatSaid { Verb = Verbs.SpeechStart} },
            };

        private readonly Dictionary<string, WhatSaid> stopListeningPhrases = new Dictionary<string, WhatSaid>
            {
                { "Stop Listening", new WhatSaid { Verb = Verbs.SpeechStop} },
            };

        private readonly Dictionary<string, WhatSaid> startScreenPhrases = new Dictionary<string, WhatSaid>
            {
                { "Play an Instrument", new WhatSaid { Verb = Verbs.Instrument} },
                { "Band Mode", new WhatSaid { Verb = Verbs.Instrument} },
                { "Play Band Mode", new WhatSaid { Verb = Verbs.Instrument} },
                { "Wall of Sound", new WhatSaid { Verb = Verbs.WallOfSound } },
                { "The Wall", new WhatSaid { Verb = Verbs.WallOfSound } },
                { "Close", new WhatSaid { Verb = Verbs.Close } },
                { "Goodbye", new WhatSaid { Verb = Verbs.Close } },
                { "Help Me", new WhatSaid { Verb = Verbs.VoiceHelp } },
            };

        private readonly Dictionary<string, WhatSaid> instrumentPhrases = new Dictionary<string, WhatSaid>
            {
                { "Lefty Acoustic Guitar", new WhatSaid { Verb = Verbs.LeftyGuitarSwitch } },
                { "Lefty Guitar", new WhatSaid { Verb = Verbs.LeftyGuitarSwitch } },
                { "Lefty Electric Guitar", new WhatSaid { Verb = Verbs.LeftyElectricGuitarSwitch } },
                { "Lefty Electric", new WhatSaid { Verb = Verbs.LeftyElectricGuitarSwitch } },
                { "Electric Guitar", new WhatSaid { Verb = Verbs.ElectricGuitarSwitch } },
                { "Acoustic Guitar", new WhatSaid { Verb = Verbs.GuitarSwitch } },
                { "Guitar", new WhatSaid { Verb = Verbs.GuitarSwitch } },
                { "Drums", new WhatSaid { Verb = Verbs.DrumsSwitch } },
                { "Keyboard", new WhatSaid { Verb = Verbs.KeyboardSwitch } },
                { "Triangle", new WhatSaid { Verb = Verbs.TriangleSwitch } },
                { "Take a Picture", new WhatSaid { Verb = Verbs.Capture } },
                { "Metronome", new WhatSaid { Verb = Verbs.StartMetronome } },
                { "Stop Metronome", new WhatSaid { Verb = Verbs.StopMetronome } },
                { "Back to Instruments", new WhatSaid { Verb = Verbs.BackToInstruments } },
                { "Go Back", new WhatSaid { Verb = Verbs.ReturnToStart } },
                { "Close", new WhatSaid { Verb = Verbs.Close } },
                { "Goodbye", new WhatSaid { Verb = Verbs.Close } },
                { "Help Me", new WhatSaid { Verb = Verbs.VoiceHelp } },
            };

        private readonly Dictionary<string, WhatSaid> booleanPhrases = new Dictionary<string, WhatSaid>
            {
                { "Yes", new WhatSaid { Verb = Verbs.True } },
                { "Yeah", new WhatSaid { Verb = Verbs.True } },
                { "Sure", new WhatSaid { Verb = Verbs.True } },
                { "Okay", new WhatSaid { Verb = Verbs.True } },
                { "No", new WhatSaid { Verb = Verbs.False } },
                { "Nope", new WhatSaid { Verb = Verbs.False } },
                { "Cancel", new WhatSaid { Verb = Verbs.False } },
                { "Stop", new WhatSaid { Verb = Verbs.False } },
            };

        private readonly Dictionary<string, WhatSaid> wallPhrases = new Dictionary<string, WhatSaid>
            {
                { "Custom", new WhatSaid { Verb = Verbs.CustomWall } },
                { "Custom Wall", new WhatSaid { Verb = Verbs.CustomWall } },
                { "Create", new WhatSaid { Verb = Verbs.CreateWall } },
                { "Record New Wall", new WhatSaid { Verb = Verbs.CreateWall } },
                { "Eight Bit", new WhatSaid { Verb = Verbs.EightBitWall } },
                { "8 Bit", new WhatSaid { Verb = Verbs.EightBitWall } },
                { "Sax", new WhatSaid { Verb = Verbs.SaxWall } },
                { "Saxophone", new WhatSaid { Verb = Verbs.SaxWall } },
                { "Metal", new WhatSaid { Verb = Verbs.MetalWall } },
                { "Trance", new WhatSaid { Verb = Verbs.TranceWall } },
                { "Animal", new WhatSaid { Verb = Verbs.AnimalWall } },
                { "Beatbox", new WhatSaid { Verb = Verbs.BeatboxWall } },
                { "Take a Picture", new WhatSaid { Verb = Verbs.Capture } },
                { "Close", new WhatSaid { Verb = Verbs.Close } },
                { "Goodbye", new WhatSaid { Verb = Verbs.Close } },
                { "Help Me", new WhatSaid { Verb = Verbs.VoiceHelp } },
            };

        private readonly Dictionary<string, WhatSaid> kinectMotorPhrases = new Dictionary<string, WhatSaid>
            {
                { "Angle Up", new WhatSaid { Verb = Verbs.KinectUp } },
                { "Angle Slightly Up", new WhatSaid { Verb = Verbs.KinectUpSmall } },
                { "Angle Down", new WhatSaid { Verb = Verbs.KinectDown } },
                { "Angle Slightly Down", new WhatSaid { Verb = Verbs.KinectDownSmall } },
            };

        private SpeechRecognitionEngine sre;
        private KinectAudioSource kinectAudioSource;
        public bool paused = true;
        private bool isDisposed;
        private bool speechEnabled = true;

        private DispatcherTimer silenceTimer;

        private SpeechRecognizer()
        {
            RecognizerInfo ri = GetKinectRecognizer();
            this.sre = new SpeechRecognitionEngine(ri);
            this.LoadGrammar(this.sre);
        }

        public event EventHandler<SaidSomethingEventArgs> SaidSomething;
        public event EventHandler<ListeningChangedEventArgs> ListeningChanged;

        public Choices startListeningChoices;
        public Choices stopListeningChoices;
        public Choices booleanChoices;
        public Choices kinectMotorChoices;
        public Choices startScreenChoices;
        public Choices instrumentChoices;
        public Choices wallChoices;

        public enum Verbs
        {
            None = 0,
            //Close Moto
            Close,
            //Return To Start Screen
            ReturnToStart,
            //Take A Picture
            Capture,
            //Confirmation
            True,
            False,
            //Help
            VoiceHelp,
            //Kinect Angle
            KinectUp,
            KinectUpSmall,
            KinectDown,
            KinectDownSmall,
            //Start/Stop Listening
            SpeechStart,
            SpeechStop,
            //Band Mode
            Instrument,
            GuitarSwitch,
            LeftyGuitarSwitch,
            ElectricGuitarSwitch,
            LeftyElectricGuitarSwitch,
            DrumsSwitch,
            KeyboardSwitch,
            TriangleSwitch,
            StartMetronome,
            StopMetronome,
            BackToInstruments,
            //Wall Of Sound
            WallOfSound,
            CustomWall,
            CreateWall,
            EightBitWall,
            SaxWall,
            TranceWall,
            MetalWall,
            AnimalWall,
            BeatboxWall,
        }

        public void changeListenState(bool state) {
            this.paused = state;
        }

        public void resetSpeechTimeout(int delay = 5, bool restart = true)
        {
            //Creates a new timer if there isn't one already, else it just renews that timer
            if (silenceTimer == null)
            {
                silenceTimer = new DispatcherTimer();
            }
            silenceTimer.Interval = TimeSpan.FromSeconds(delay);

            if (!silenceTimer.IsEnabled)
            {
                silenceTimer.Tick += new EventHandler(silenceTimer_Tick);
            }
          
            if (restart)
            {
                silenceTimer.Start();
            }
        }

        void disableSpeechTimeout()
        {
            if (silenceTimer != null)
            {
                silenceTimer.Stop();
                silenceTimer.Tick -= new EventHandler(silenceTimer_Tick);
                silenceTimer = null;
            }
        }

        void silenceTimer_Tick(object sender, EventArgs e)
        {
            //Stop listening for ticks and remove all the references to it. Also alert Moto that the listen state has changed.
            disableSpeechTimeout();
            toggleListening(false);
        }

        public EchoCancellationMode EchoCancellationMode
        {
            get
            {
                this.CheckDisposed();

                return this.kinectAudioSource.EchoCancellationMode;
            }

            set
            {
                this.CheckDisposed();

                this.kinectAudioSource.EchoCancellationMode = value;
            }
        }

        // This method exists so that it can be easily called and return safely if the speech prereqs aren't installed.
        // We isolate the try/catch inside this class, and don't impose the need on the caller.
        public static SpeechRecognizer Create()
        {
            SpeechRecognizer recognizer = null;

            try
            {
                recognizer = new SpeechRecognizer();
            }
            catch (Exception)
            {
                // speech prereq isn't installed. a null recognizer will be handled properly by the app.
            }

            return recognizer;
        }

        public void Start(KinectAudioSource kinectSource)
        {
            this.CheckDisposed();

            this.kinectAudioSource = kinectSource;
            this.kinectAudioSource.NoiseSuppression = true;
            this.kinectAudioSource.EchoCancellationMode = Microsoft.Kinect.EchoCancellationMode.CancellationAndSuppression;
            this.kinectAudioSource.AutomaticGainControlEnabled = false;
            this.kinectAudioSource.BeamAngleMode = BeamAngleMode.Adaptive;
            var kinectStream = this.kinectAudioSource.Start();
            this.sre.SetInputToAudioStream(
                kinectStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            this.sre.RecognizeAsync(RecognizeMode.Multiple);
        }

        public void switchGrammar(Choices[] choices, bool prefixed = true, bool startListeningKeywords = true) {
            //this.sre.RecognizeAsyncStop();
            this.sre.UnloadAllGrammars();
            Console.WriteLine("GRAMMARLOL");

            if (prefixed)
            {
                var gb = new GrammarBuilder(startListeningChoices);
                Choices allChoices = new Choices();
                
                foreach (var c in choices)
                {
                    allChoices.Add(c);
                }

                gb.Append(allChoices);

                this.sre.LoadGrammarAsync(new Grammar(gb));
            }
            else
            {
                foreach (var g in choices)
                {
                    this.sre.LoadGrammarAsync(new Grammar(g));
                }
            }

            if (startListeningKeywords)
            {
                this.sre.LoadGrammarAsync(new Grammar(startListeningChoices));
            }
        }

        public void Stop()
        {
            this.CheckDisposed();

            if (this.sre != null)
            {
                this.kinectAudioSource.Stop();
                this.sre.RecognizeAsyncCancel();
                this.sre.RecognizeAsyncStop();

                this.sre.SpeechRecognized -= this.SreSpeechRecognized;
                this.sre.SpeechHypothesized -= this.SreSpeechHypothesized;
                this.sre.SpeechRecognitionRejected -= this.SreSpeechRecognitionRejected;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "sre",
            Justification = "This is suppressed because FXCop does not see our threaded dispose.")]
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Stop();

                if (this.sre != null)
                {
                    // NOTE: The SpeechRecognitionEngine can take a long time to dispose
                    // so we will dispose it on a background thread
                    ThreadPool.QueueUserWorkItem(
                        delegate(object state)
                        {
                            IDisposable toDispose = state as IDisposable;
                            if (toDispose != null)
                            {
                                toDispose.Dispose();
                            }
                        },
                            this.sre);
                    this.sre = null;
                }

                this.isDisposed = true;
            }
        }

        private static RecognizerInfo GetKinectRecognizer()
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }

        private void CheckDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("SpeechRecognizer");
            }
        }

        private void LoadGrammar(SpeechRecognitionEngine speechRecognitionEngine)
        {
            startListeningChoices = new Choices();
            foreach (var phrase in this.startListeningPhrases)
            {
                startListeningChoices.Add(phrase.Key);
            }

            stopListeningChoices = new Choices();
            foreach (var phrase in this.stopListeningPhrases)
            {
                stopListeningChoices.Add(phrase.Key);
            }

            booleanChoices = new Choices();
            foreach (var phrase in this.booleanPhrases)
            {
                booleanChoices.Add(phrase.Key);
            }

            kinectMotorChoices = new Choices();
            foreach (var phrase in this.kinectMotorPhrases)
            {
                kinectMotorChoices.Add(phrase.Key);
            }

            startScreenChoices = new Choices();
            foreach (var phrase in this.startScreenPhrases)
            {
                startScreenChoices.Add(phrase.Key);
            }

            instrumentChoices = new Choices();
            foreach (var phrase in this.instrumentPhrases)
            {
                instrumentChoices.Add(phrase.Key);
            }

            wallChoices = new Choices();
            foreach (var phrase in this.wallPhrases)
            {
                wallChoices.Add(phrase.Key);
            }

            /*
             * ADD NEW GRAMMARS HERE
             * Copy code from above, and place it just above this comment
             * Amend "allChoices" to add the new dictionary
             * Add to "allDicts" further down
             */

            var allChoices = new Choices();
            allChoices.Add(startScreenChoices);
            allChoices.Add(kinectMotorChoices);

            // This is needed to ensure that it will work on machines with any culture, not just en-us.
            var gb = new GrammarBuilder(startListeningChoices) { Culture = speechRecognitionEngine.RecognizerInfo.Culture };
            gb.Append(allChoices);

            var g = new Grammar(gb);
            var g2 = new Grammar(startListeningChoices);
            speechRecognitionEngine.LoadGrammarAsync(g);
            speechRecognitionEngine.LoadGrammarAsync(g2);
            speechRecognitionEngine.SpeechRecognized += this.SreSpeechRecognized;
            speechRecognitionEngine.SpeechHypothesized += this.SreSpeechHypothesized;
            speechRecognitionEngine.SpeechRecognitionRejected += this.SreSpeechRecognitionRejected;
        }

        private void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            /*if (speechEnabled)
            {
                var said = new SaidSomethingEventArgs { Verb = Verbs.None, Matched = "?" };

                if (this.SaidSomething != null)
                {
                    this.SaidSomething(new object(), said);
                }

                Console.WriteLine("\nSpeech Rejected");
            }*/
        }

        private void SreSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            if (speechEnabled)
            {
                Console.WriteLine("\rSpeech Hypothesized: \t{0} \t{1}", e.Result.Text, e.Result.Confidence);

                if (e.Result.Confidence > 0.4)
                {
                    //It's possible they said this, they just need to repeat it
                    if (!this.paused)
                    {
                        resetSpeechTimeout();
                    }
                }
            }
        }

        private void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (speechEnabled)
            {
                Console.WriteLine("\rSpeech Recognized: \t{0} - \t{1}", e.Result.Text, e.Result.Confidence);

                if ((this.SaidSomething == null) || (e.Result.Confidence < 0.55))
                {
                    return;
                }

                if (!this.paused)
                {
                    resetSpeechTimeout(); //Reset the silence timeout if we think something was said
                }

                var said = new SaidSomethingEventArgs { Verb = 0, Phrase = e.Result.Text };

                // Look for a match in the order of the lists below, first match wins.
                List<Dictionary<string, WhatSaid>> allDicts = new List<Dictionary<string, WhatSaid>> { this.startListeningPhrases, this.stopListeningPhrases, this.booleanPhrases, this.kinectMotorPhrases, this.startScreenPhrases, this.instrumentPhrases, this.wallPhrases };

                bool found = false;
                for (int i = 0; i < allDicts.Count && !found; ++i)
                {
                    foreach (var phrase in allDicts[i])
                    {
                        if (e.Result.Text.EndsWith(phrase.Key))
                        {
                            said.Verb = phrase.Value.Verb;
                            said.Matched = phrase.Key;
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    return;
                }

                if (this.paused)
                {
                    // Only accept start listening
                    if ((said.Verb == Verbs.SpeechStart))
                    {
                        toggleListening(true);
                    }
                }
                else
                {
                    if (said.Verb == Verbs.SpeechStart)
                    {
                        return;
                    }


                    if (said.Verb == Verbs.SpeechStop)
                    {
                        toggleListening(false);
                    }
                }

                if (this.SaidSomething != null)
                {
                    this.SaidSomething(new object(), said);
                }
            }
        }

        private struct WhatSaid
        {
            public Verbs Verb;
        }

        public class SaidSomethingEventArgs : EventArgs
        {
            public Verbs Verb { get; set; }

            public string Phrase { get; set; }

            public string Matched { get; set; }
        }

        public class ListeningChangedEventArgs : EventArgs
        {
            public bool Paused { get; set; }
        }

        private Image microphoneImg;

        public void startListening(Canvas canvas)
        {
            //Shows the microphone icon on screen at the appropriate time
            microphoneImg = new Image();
            BitmapImage BitImg = new BitmapImage(new Uri(
                "/Moto;component/images/microphone.png", UriKind.Relative));
            microphoneImg.Source = BitImg;
            microphoneImg.Height = 50;

            canvas.Children.Add(microphoneImg);

            Canvas.SetTop(microphoneImg, (canvas.ActualHeight - microphoneImg.Height - 15));
            Canvas.SetLeft(microphoneImg, 15);

            MainWindow.animateSlide(microphoneImg,false,true,10,0.5);

        }

        public void toggleListening(bool listening)
        {
            //Changes the 'paused' value and lets Moto know it's been changed
            if (listening)
            {
                resetSpeechTimeout();
                this.paused = false;
            }
            else
            {
                disableSpeechTimeout();
                this.paused = true;
            }

            if (this.ListeningChanged != null)
            {
                this.ListeningChanged(this, new ListeningChangedEventArgs { Paused = this.paused });
            }
        }

        public void stopListening(Canvas canvas)
        {
            disableSpeechTimeout();

            if (microphoneImg != null)
            {
                MainWindow.animateSlide(microphoneImg, true, true, 10, 0.5);
            }

            //canvas.Children.Remove(microphoneImg);
        }

        public void toggleSpeech()
        {
            if (speechEnabled)
            {
                speechEnabled = false;
            }
            else
            {
                speechEnabled = true;
            }

            Console.WriteLine("Speech Recognition: " + speechEnabled);
        }

        public void speechEnabledSwitch(bool on)
        {
            if (on)
            {
                speechEnabled = true;
            }
            else
            {
                speechEnabled = false;
            }

            Console.WriteLine("Speech Recognition: " + speechEnabled);
        }
    }
}