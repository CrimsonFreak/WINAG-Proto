# WINAG-Proto
## GameManager : MonoBehavior
The GameManager Starts the game by instantiating **Sections** based on Static data defined in the menu screen. 
It Holds variables that needs to be accessed by multiple scripts and the first level of camera and selection by click.

### Properties 
| Type | Name | Desciption |
| ------------- | ------------- | ------------- |
| int  | NumberOfTurns  | The total number of turns for that game. |
| int  | CurrentTurn  | Increment for the turn currently played at. |
| int  | SectionNumber | Length of the Section Array in static data. Used to instantiate Sections in the game. **Needs refactoring** |
| Camera  | MapView | The Camera for the N+1 view (IGN Map) |
| Camera  | SceneVeiw | The Camera for the N view (3D View) |
...


### Methods

| Name | Desciption |
| ------------- | ------------- |
| Start  | Instatiate the sections. |
| LateStart  | Coroutine that fires after all UI elements are up and running. |
| Update | Raycasts in the active view to check for object selection. Starts the Resolving Phase (realtime action) Stack if resolvingPhaseInputs is not null.|
...

FlowChart Of Objects
https://drive.google.com/file/d/1rZUwOXEJIsm3gBm7-J9uXlb78LeADOWp/view?usp=sharing
