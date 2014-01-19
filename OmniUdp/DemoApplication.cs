﻿using System;
using System.Threading;
using PCSC;
using log4net;

namespace OmniUdp {
  internal class DemoApplication : Application {
    /// <summary>
    ///   The logging <see langword="interface" />
    /// </summary>
    private readonly ILog Log = LogManager.GetLogger( System.Reflection.MethodBase.GetCurrentMethod().DeclaringType );

    public DemoApplication( string networkInterface, string ipAddress, bool useLoopback, string identifier, bool ascii ) : base( networkInterface, ipAddress, useLoopback, identifier, ascii ) {}

    /// <summary>
    ///   Create UID reader context and wait for events.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///   There are currently no readers installed.
    /// </exception>
    protected override void ExecuteContext() {
      // Retrieve the names of all installed readers.
      using( Context = new SCardContext() ) {
        Context.Establish( SCardScope.System );

        string[] readernames = null;
        try {
          readernames = Context.GetReaders();
        } catch( PCSCException ) {} finally {
          if( null == readernames || 0 == readernames.Length ) {
            Log.Error( "There are currently no readers installed." );
          }
        }

        // Create a monitor object with its own PC/SC context.
        SCardMonitor monitor = new SCardMonitor( new SCardContext(), SCardScope.System );

        // Point the callback function(s) to the static defined methods below.
        monitor.CardInserted += CardInserted;

        if( null != readernames ) {
          foreach( string reader in readernames ) {
            Log.InfoFormat( "Start monitoring for reader '{0}'.", reader );
          }
          monitor.Start( readernames );
        }

        // Wait for the parent application to signal us to exit.
        ExitApplication = new ManualResetEvent( false );
        ExitApplication.WaitOne();

        // Stop monitoring
        monitor.Cancel();
      }
    }

    /// <summary>
    ///   Handle keyboard input from the main application.
    /// </summary>
    /// <param name="key">The key information that was recorded.</param>
    public override void HandleKeyboardInput( ConsoleKeyInfo key ) {
      base.HandleKeyboardInput( key );

      switch( key.Key ) {
        case ConsoleKey.D0:
          SimulateCardEvent( new byte[] {0, 0, 0, 0} );
          break;
        default:
          break;
      }
    }

    /// <summary>
    /// Simulate a card being inserted and process the given UID as if it was present in the reader.
    /// </summary>
    /// <param name="shortUid">The UID as it was supposed to be read from the reader.</param>
    private void SimulateCardEvent( byte[] shortUid ) {
      Log.Info( "Simulating card detected." );
      try {
        string uidString = BitConverter.ToString( shortUid ).Replace( "-", string.Empty );
        Log.InfoFormat( "Read UID '{0}' from '{1}'.", uidString, "<simulated>" );
        BroadcastUidEvent( shortUid );
      } catch( Exception ex ) {
        Log.Error( ex.Message );
        BroadcastErrorEvent( new byte[] {0} );
      }
    }
  }
}