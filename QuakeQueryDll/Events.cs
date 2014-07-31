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
            ReceiveEventHandler handler = NewServerEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
        /// <summary>
        /// GetInfo Event
        /// </summary>
        public event ReceiveEventHandler infoResponseEvent;
        internal virtual void OnInfoResponse(Server sender)
        {
            ReceiveEventHandler handler = infoResponseEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
        /// <summary>
        /// GetStatus Event
        /// </summary>
        public event ReceiveEventHandler statusResponseEvent;
        internal virtual void OnStatusResponse(Server sender)
        {
            ReceiveEventHandler handler = statusResponseEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
        /// <summary>
        /// GetServers Event
        /// </summary>
        public event ReceiveEventHandler masterResponseEvent;
        internal virtual void OnMasterResponse(Server sender)
        {
            ReceiveEventHandler handler = masterResponseEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
        /// <summary>
        /// (rcon) Print Event
        /// </summary>
        public event ReceiveEventHandler printResponseEvent;
        internal virtual void OnPrintResponse(Server sender)
        {
            ReceiveEventHandler handler = printResponseEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
        /// <summary>
        /// Not Expected Respnose Event
        /// </summary>
        public event ReceiveEventHandler otherResponseEvent;
        internal virtual void OnOtherResponse(Server sender)
        {
            ReceiveEventHandler handler = otherResponseEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
        /// <summary>
        /// Get Any Response Event
        /// </summary>
        public event ReceiveEventHandler serverResponseEvent;
        internal virtual void OnServerResponse(Server sender)
        {
            ReceiveEventHandler handler = serverResponseEvent;
            if (handler != null)
            {
                handler(sender);
            }
        }
    }
}
