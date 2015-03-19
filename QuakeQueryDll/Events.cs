namespace QuakeQueryDll
{
    public partial class QuakeQuery
    {
        public delegate void ReceiveEventHandler(Server sender);
        /// <summary>
        /// New Valid Server IP found
        /// </summary>
        public event ReceiveEventHandler NewServerEvent;
        internal virtual void OnNewServerResponse(Server sender)
        {
            var handler = NewServerEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
        /// <summary>
        /// GetInfo Event
        /// </summary>
        public event ReceiveEventHandler InfoResponseEvent;
        internal virtual void OnInfoResponse(Server sender)
        {
            var handler = InfoResponseEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
        /// <summary>
        /// GetStatus Event
        /// </summary>
        public event ReceiveEventHandler StatusResponseEvent;
        internal virtual void OnStatusResponse(Server sender)
        {
            var handler = StatusResponseEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
        /// <summary>
        /// GetServers Event
        /// </summary>
        public event ReceiveEventHandler MasterResponseEvent;
        internal virtual void OnMasterResponse(Server sender)
        {
            var handler = MasterResponseEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
        /// <summary>
        /// (rcon) Print Event
        /// </summary>
        public event ReceiveEventHandler PrintResponseEvent;
        internal virtual void OnPrintResponse(Server sender)
        {
            var handler = PrintResponseEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
        /// <summary>
        /// Not Expected Respnose Event
        /// </summary>
        public event ReceiveEventHandler OtherResponseEvent;
        internal virtual void OnOtherResponse(Server sender)
        {
            var handler = OtherResponseEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
        /// <summary>
        /// Get Any Response Event
        /// </summary>
        public event ReceiveEventHandler ServerResponseEvent;
        internal virtual void OnServerResponse(Server sender)
        {
            var handler = ServerResponseEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
        /// <summary>
        /// Get Any Response Event
        /// </summary>
        public event ReceiveEventHandler GotCvarEvent;
        internal virtual void OnCvarSuccess(Server sender)
        {
            var handler = GotCvarEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
    }
}
