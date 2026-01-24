using SPAD.neXt.Interfaces;
using SPAD.neXt.Interfaces.Configuration;
using SPAD.neXt.Interfaces.Events;
using SPAD.neXt.Interfaces.Logging;
using SPAD.neXt.Interfaces.Scripting;
using SPAD.neXt.Interfaces.Scripting.Stubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace snAudioControl
{
    public class AudioControlAction : ScriptStub, IScriptAction2,IHasID
    {
        public int NumberOfParameters => 1;

        protected override string ScriptDataPrefix => "SPAD_AUDIOCONTROL";
        private readonly static Guid _ID = new Guid("{7EE6AF9D-8E09-4C5A-95A9-F79971DB1FC1}");
        public Guid ID => _ID;

        private IDataDefinition volumeData = null;
        public void Execute(IApplication app, List<IEventActionParameter> actionParameters)
        {
            if (actionParameters != null && actionParameters.Count == 1)
            {
                var firstParameter = actionParameters.First().GetParameterValue<Int32>(0);
                try
                {
                    AudioManager.SetMasterVolume(Math.Min(Math.Max(0, firstParameter), 100));
                }
                catch (Exception ex) { ScriptLogger.Warn(ex.ToString()); }
            }
        }

        public void Execute(IApplication app, ISPADEventArgs eventArgs)
        {
            throw new NotImplementedException();
        }

        protected override void DeinitializeScript()
        {

        }
        private Task workerTask = null;
        private IObserverTicket ticket = null;
        protected override void InitializeScript()
        {
            _CurrentAudioValue = AudioManager.GetMasterVolume();
            volumeData = GetOrCreateScriptDataValue("VOLUME",_CurrentAudioValue);// Creates LOCAL:SPAD_AUDIOCONTROL_VOLUME            
            ticket = EventSystem.CreateObserverTicket("AUDIOCONTROL", "VOLUMECHANGED", null);
            ticket.ObservedValueChanged += Ticket_ObservedValueChanged;
            ticket.Subscribe(volumeData.Name);
            workerTask = Task.Run(workerThreadAction, CancelToken);
        }

        private void Ticket_ObservedValueChanged(object sender, ObserverTicketArgs e)
        {
            try
            {
                var t = Convert.ToSingle(e.SPADEventArgs.NewValue);
                if (e.SPADEventArgs.Sender != ID && t != _CurrentAudioValue)
                {
                    ScriptLogger.Info("Setmastervolume {0}", t);
                    AudioManager.SetMasterVolume(Math.Min(Math.Max(0, t), 100));
                }
            }
            catch (Exception ex) { ScriptLogger.Warn(ex.ToString()); }
            e.Handled = true;
        }
        volatile float _CurrentAudioValue = -2f;
        private async Task workerThreadAction()
        {
            while (!CancelToken.IsCancellationRequested)
            {
                if (Application.ApplicationIsReady)
                {
                    var newVal = -999f;
                    try
                    {
                        newVal = AudioManager.GetMasterVolume();
                        if (newVal != _CurrentAudioValue)
                        {
                            lock (ticket)
                                _CurrentAudioValue = newVal;
                            volumeData.Monitorable.SetValue(newVal,0,ID);                       
                        }
                    }
                    catch (Exception ex)
                    {
                        ScriptLogger.Error(ex.ToString());
                    }
                }
                await Task.Delay(500);
            }
        }
    }
}
