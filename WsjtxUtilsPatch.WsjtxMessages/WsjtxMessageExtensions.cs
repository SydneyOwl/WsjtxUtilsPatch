﻿using System;
using WsjtxUtilsPatch.WsjtxMessages.Messages;

namespace WsjtxUtilsPatch.WsjtxMessages
{
    /// <summary>
    /// Extension methods used to create and write WSJT-X messages
    /// </summary>
    public static class WsjtxMessageExtensions
    {
        /// <summary>
        /// Create a default message for the given WSJT-X message type
        /// </summary>
        /// <param name="messageType">The type of WSJT-X message</param>
        /// <exception cref="ArgumentException">Exception thrown if the <see cref="MessageType"/> is an unknown message type</exception>
        /// <returns>A default WSJT-X message</returns>
        public static WsjtxMessage? CreateDefaultMessage(this MessageType messageType)
        {
            Type type = messageType switch
            {
                MessageType.Clear => typeof(Clear),
                MessageType.Close => typeof(Close),
                MessageType.Configure => typeof(Configure),
                MessageType.Decode => typeof(Decode),
                MessageType.FreeText => typeof(FreeText),
                MessageType.HaltTx => typeof(HaltTx),
                MessageType.Heartbeat => typeof(Heartbeat),
                MessageType.HighlightCallsign => typeof(HighlightCallsign),
                MessageType.Location => typeof(Location),
                MessageType.LoggedADIF => typeof(LoggedAdif),
                MessageType.QSOLogged => typeof(QsoLogged),
                MessageType.Replay => typeof(Replay),
                MessageType.Reply => typeof(Reply),
                MessageType.Status => typeof(Status),
                MessageType.SwitchConfiguration => typeof(SwitchConfiguration),
                MessageType.WSPRDecode => typeof(WSPRDecode),
                _ => throw new ArgumentException($"The type {messageType} is an unknown WSJT-X message type."),
            };
            return Activator.CreateInstance(type) as WsjtxMessage;
        }

        /// <summary>
        /// Create a WSJT-X message from the source memory
        /// </summary>
        /// <param name="source">Source memory</param>
        /// <exception cref="NotImplementedException">Exception thrown if the message type does not implement <see cref="IWsjtxDirectionOut"/></exception>
        /// <returns>A WSJT-X message</returns>
        public static WsjtxMessage? DeserializeWsjtxMessage(this Memory<byte> source)
        {
            var reader = new WsjtxMessageReader(source);
            MessageType messageType = reader.PeekMessageType();
            var result = messageType.CreateDefaultMessage();
            
            if (result is null)
                return result;

            if (result is not IWsjtxDirectionOut)
                throw new NotImplementedException(
                    $"The message type {messageType} does not implement IWsjtxDirectionOut");

            result.ReadMessage(reader);
            return result;
        }

        /// <summary>
        /// Write a WSJT-X message to the destination memory
        /// </summary>
        /// <typeparam name="T">Type of WSJT-X message</typeparam>
        /// <param name="message">The target WSJT-X message</param>
        /// <param name="dest">The target memory</param>
        /// <returns>The number of bytes written</returns>
        public static int WriteMessageTo<T>(this T message, Memory<byte> dest) where T : WsjtxMessage, IWsjtxDirectionIn
        {
            WsjtxMessageWriter writer = new(dest);
            message.WriteMessage(writer);
            return writer.Position;
        }
    }
}