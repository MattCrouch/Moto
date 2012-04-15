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
        private readonly Dictionary<string, WhatSaid> startScreenPhrases = new Dictionary<string, WhatSaid>
            {
                { "Kinect", new WhatSaid { Verb = Verbs.SpeechStart} },
                { "Moto", new WhatSaid { Verb = Verbs.SpeechStart} },
                { "Mowtow", new WhatSaid { Verb = Verbs.SpeechStart} },
                { "Stop Listening", new WhatSaid { Verb = Verbs.SpeechStop} },
                { "Play an Instrument", new WhatSaid { Verb = Verbs.Instrument} },
                { "Band Mode", new WhatSaid { Verb = Verbs.Instrument} },
                { "Play Band Mode", new WhatSaid { Verb = Verbs.Instrument} },
                { "Wall of Sound", new WhatSaid { Verb = Verbs.WallOfSound } },
                { "The Wall", new WhatSaid { Verb = Verbs.WallOfSound } },
                { "Close", new WhatSaid { Verb = Verbs.Close } },
                { "Goodbye", new WhatSaid { Verb = Verbs.Close } },
            };

        private readonly Dictionary<string, WhatSaid> instrumentPhrases = new Dictionary<string, WhatSaid>
            {
                { "Switch to Guitar", new WhatSaid { Verb = Verbs.GuitarSwitch } },
                { "Switch to Drums", new WhatSaid { Verb = Verbs.DrumsSwitch } },
                { "Take a Picture", new WhatSaid { Verb = Verbs.Capture } },
                { "Metronome", new WhatSaid { Verb = Verbs.StartMetronome } },
                { "Stop Metronome", new WhatSaid { Verb = Verbs.StopMetronome } },
                { "Back to Instruments", new WhatSaid { Verb = Verbs.BackToInstruments } },
                { "Go Back", new WhatSaid { Verb = Verbs.ReturnToStart } },
            };

        private readonly Dictionary<string, WhatSaid> booleanPhrases = new Dictionary<string, WhatSaid>
            {
                { "Yes", new WhatSaid { Verb = Verbs.True } },
                { "Yeah", new WhatSaid { Verb = Verbs.True } },
                { "Sure", new WhatSaid { Verb = Verbs.True } },
                { "Okay", new WhatSaid { Verb = Verbs.True } },
                { "Continue", new WhatSaid { Verb = Verbs.True } },
                { "No", new WhatSaid { Verb = Verbs.False } },
                { "Nope", new WhatSaid { Verb = Verbs.False } },
                { "Cancel", new WhatSaid { Verb = Verbs.False } },
                { "Stop", new WhatSaid { Verb = Verbs.False } },
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
        private bool paused = true;
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

        public enum Verbs
        {
            None = 0,
            True,
            False,
            SpeechStart,
            SpeechStop,
            Instrument,
            WallOfSound,
            Close,
            Capture,
            GuitarSwitch,
            DrumsSwitch,
            StartMetronome,
            StopMetronome,
            BackToInstruments,
            ReturnToStart,
            KinectUp,
            KinectUpSmall,
            KinectDown,
            KinectDownSmall,
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
            silenceTimer.Interval = new TimeSpan(0, 0, delay);

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
                if (silenceTimer.IsEnabled)
                {
                    silenceTimer.Stop();
                }

                silenceTimer.Tick -= new EventHandler(silenceTimer_Tick);
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
            this.kinectAudioSource.EchoCancellationMode = Microsoft.Kinect.EchoCancellationMode.CancellationOnly;
            this.kinectAudioSource.AutomaticGainControlEnabled = false;
            this.kinectAudioSource.BeamAngleMode = BeamAngleMode.Adaptive;
            var kinectStream = this.kinectAudioSource.Start();
            this.sre.SetInputToAudioStream(
                kinectStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            this.sre.RecognizeAsync(RecognizeMode.Multiple);
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
            // Build a simple grammar of shapes, colors, and some simple program control
            var startScreen = new Choices();
            foreach (var phrase in this.startScreenPhrases)
            {
                startScreen.Add(phrase.Key);
            }

            var boolean = new Choices();
            foreach (var phrase in this.booleanPhrases)
            {
                boolean.Add(phrase.Key);
            }

            var instrument = new Choices();
            foreach (var phrase in this.instrumentPhrases)
            {
                instrument.Add(phrase.Key);
            }

            var kinectMotor = new Choices();
            foreach (var phrase in this.kinectMotorPhrases)
            {
                kinectMotor.Add(phrase.Key);
            }

            /*
             * ADD NEW GRAMMARS HERE
             * Copy code from above, and place it just above this comment
             * Amend "allChoices" to add the new dictionary
             * Add to "allDicts" further down
             */

            var allChoices = new Choices();
            allChoices.Add(startScreen);
            allChoices.Add(boolean);
            allChoices.Add(instrument);
            allChoices.Add(kinectMotor);

            // This is needed to ensure that it will work on machines with any culture, not just en-us.
            var gb = new GrammarBuilder { Culture = speechRecognitionEngine.RecognizerInfo.Culture };
            gb.Append(allChoices);

            var g = new Grammar(gb);
            speechRecognitionEngine.LoadGrammar(g);
            speechRecognitionEngine.SpeechRecognized += this.SreSpeechRecognized;
            speechRecognitionEngine.SpeechHypothesized += this.SreSpeechHypothesized;
            speechRecognitionEngine.SpeechRecognitionRejected += this.SreSpeechRecognitionRejected;
        }

        private void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            if (speechEnabled)
            {
                var said = new SaidSomethingEventArgs { Verb = Verbs.None, Matched = "?" };

                if (this.SaidSomething != null)
                {
                    this.SaidSomething(new object(), said);
                }

                Console.WriteLine("\nSpeech Rejected");
            }
        }

        private void SreSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            if (speechEnabled)
            {
                Console.WriteLine("\rSpeech Hypothesized: \t{0}", e.Result.Text);
            }
        }

        private void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (speechEnabled)
            {
                Console.WriteLine("\rSpeech Recognized: \t{0} - \t{1}", e.Result.Text, e.Result.Confidence);

                if ((this.SaidSomething == null) || (e.Result.Confidence < 0.3))
                {
                    return;
                }

                resetSpeechTimeout(); //Reset the silence timeout if we think something was said

                var said = new SaidSomethingEventArgs { Verb = 0, Phrase = e.Result.Text };

                // Look for a match in the order of the lists below, first match wins.
                List<Dictionary<string, WhatSaid>> allDicts = new List<Dictionary<string, WhatSaid>> { this.startScreenPhrases, this.booleanPhrases, this.instrumentPhrases, this.kinectMotorPhrases };

                bool found = false;
                for (int i = 0; i < allDicts.Count && !found; ++i)
                {
                    foreach (var phrase in allDicts[i])
                    {
                        if (e.Result.Text == phrase.Key)
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
                    // Only accept restart or reset
                    if ((said.Verb != Verbs.SpeechStart) && (said.Verb != Verbs.SpeechStop))
                    {
                        return;
                    }

                    toggleListening(true);
                }
                else
                {
                    if (said.Verb == Verbs.SpeechStart)
                    {
                        return;
                    }
                }

                if (said.Verb == Verbs.SpeechStop)
                {
                    toggleListening(false);
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

            MainWindow.animateSlide(microphoneImg,true,false,10,0.5);

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
    }
}