// MarrowMachine CONFIDENTIAL
// __________________
// 
// [2016] - [2023] MarrowMachine LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of MarrowMachine LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// herein are proprietary to MarrowMachine LLC
// and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from MarrowMachine LLC.

using Zenject;

namespace Stability
{
    public class StabilityInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.DeclareSignal<StabilitySignals.HandleError>();
            Container.DeclareSignal<StabilitySignals.InitializeStability>();
        }
    }
}