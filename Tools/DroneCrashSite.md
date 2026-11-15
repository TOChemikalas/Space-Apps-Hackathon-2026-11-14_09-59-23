# Drone discovery

SampleScene contains a DroneCrashSite component referencing the main Life systems building. It places the crash at base-relative (3000, 0) metres, using one world unit per metre. The rover-folder lo.png supplies the crash artwork, with a small collision footprint. Copies of the existing ground extend the route east without rescaling the original base terrain.

The SURFACE SURVEY message is authored into CommunicationsSession with stable ID `survey.drone_crash`. It waits for a powered, aligned communications link, appears once, and uses the existing save/read system. Received unread messages show the supplied sunny.jpg icon below the HUD. No sunny.png image was present; its orphan metadata was left intact.

Opening the discovery message in the inbox marks it read and activates the red minimap direction arrow and distance. The arrow stays within the minimap, follows the rover when occupied, and uses the base as the exterior departure point while the astronaut is in a separate interior. Near the site the label changes to “Crash site reached.” Saved read state restores tracking without resending the notification.

All game scripts were compiled against the installed Unity engine and package assemblies with zero errors. Communications regression checks cover offline queuing, unread delivery, reading and persistence. Rendered appearance, terrain seams, mouse interaction and the 3 km journey were not Play Mode tested. Travel speed, suit reserves and rover consumption have not been changed; this distance may require further travel balancing.
