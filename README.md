# ElevatorChallenge_Sln

This is a solution for the Elevator Challenge, which is a coding exercise that involves simulating the behavior of an elevator system. 
The challenge typically includes requirements such as handling multiple requests, managing elevator states, and optimizing the movement of the elevator.

For the purposes of this solution, the following is implemented:
- The solution includes an appsettings.json file that contains configuration settings for the elevator system, such as the number of elevators, the number of floors, and other parameters.
- If the required settings are not present in the appsettings.json file, the user will be prompted to enter them via the console when the application is run.
- The solution is designed to be extensible, allowing for easy addition of new features or modifications to existing functionality.
- Serilog is used for logging and the log can be found in the logs folder just off the root of the solution.
- Dependency Injection is used to manage the dependencies of the application, making it easier to test and maintain and to ensure adherence to the SOLID principles.
- This solution can handle multiple elevator requests concurrently, optimizing the queue and movement based on current elevator states.
