# How to embed Lifetime into your C# project

## Step 1: Include it in your project
### (Recommended) Method 1: Release DLL approach
> **NOTE:** .NET 8.0 is recommended for Lifetime, newer versions of .NET should work but 8.0 is the minimum.
1. Download any ZIP from the version of your choice from the release tab (doesn't really matter, we just need the DLL)
2. Extract liblifetime.dll from the ZIP into your project directory
3. Include liblifetime.dll in your project by putting the following into your csproj file:
    ```xml
    <ItemGroup>
        <Reference Include="liblifetime">
            <HintPath>path/to/liblifetime.dll</HintPath>
        </Reference>
    </ItemGroup>
    ```

### Method 2: Included project approach
> **NOTE:** The .NET 8.0 SDK is required for this method. You can also modify the csproj to use your version of the SDK, but lowering the version will require refactoring the code to work in that version of .NET.
1. Download the source code of the version of your choice from the release tab, or clone the GitHub repo
2. Copy the liblifetime folder into your solution directory
3. Add the project into your solution and include it in your project:
    ```sh
    dotnet sln add path/to/liblifetime
    cd yourproject
    dotnet add project path/to/liblifetime
    ```

## Step 2: Add it into your code
1. Include the namespace:
    ```cs
    using Mattodev.Lifetime;
    ```
2. Create a runtime container (an object with all of the data necessary to operate the interpreter):
    ```cs
    // DefaultContainer() creates a container with all standard functions
    LTRuntimeContainer rtContainer = LTInterpreter.DefaultContainer();
    ```
3. Add console input/output handlers:
    ```cs
    rtContainer.InputHandler += question => {
        Console.Write(question);
        return Console.ReadLine() ?? "";
    };

    rtContainer.OutputHandler += Console.Write;

    rtContainer.ErrOutputHandler += message => {
        ConsoleColor oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(message);
        Console.ForegroundColor = oldColor;
    };
    ```
4. Execute your code!
    ```cs
    // if result is true, all went well
    // if result is false, something errored
    bool result = LTInterpreter.Exec(
        ["!sys->io::print_line \"hello, world!\""], // source code in an array
        "hello.lt", // file name
        ref rtContainer // reference to the runtime container
    );
    ```

# Other useful tasks
## Create and append C# functions to Lifetime (`LTInternalFunc`)
1. Create the function:
    ```cs
    LTInternalFunc function = new(
        "greet", // function name
        "my_namespace", // namespace
        "my_class", // class
        "obj", // return type (str - string, int32 - integer, obj - anything else)
        LTVarAccess.Public, // function access, Public - accessible from everywhere, Private - accessible from only the same namespace and class
        [("str", "name")], // argument list, pairs of ("type", "argument_name")
        false, // false - checks argument count before function execution, true - doesn't check and fills missing spots with null
        (rtContainer, argumentList) => {
            // get arguments either by their index - argumentList[0] - or by their name, like this:
            LTVar nameVar = argumentList["name"];
            // to get this variable's value, use the Value property:
            string name = nameVar.Value;
            // to log a message to the specified rtContainer, use this function:
            LTInterpreter.LogMsg($"Hello, {name}!\n", ref rtContainer);
            // and what do we return?
            return (
                null, // the return value, null if nothing is returned (will be converted to the Lifetime equivalent automatically)
                null, // an error if one occured, null if all went well
                rtContainer // and the runtime container itself
            );
        }
    );
    ```
2. Append it to the runtime container:
    ```cs
    rtContainer.AppendIFunc(function);
    ```
   > **NOTE:** Although there is a `IFuncs` property in a runtime container, do not manually append to it. (To be honest, I've made this sound threatening, but worst case scenario: your function won't be found.)
3. Congrats, it can now be called from Lifetime!

## Create and append Lifetime functions from C# (`LTDefinedFunc`)
1. Create the function:
    ```cs
    LTDefinedFunc function = new(
        "greet", // function name
        "my_namespace", // namespace
        "my_class", // class
        "obj", // return type (str - string, int32 - integer, obj - anything else)
        LTVarAccess.Public, // function access, Public - accessible from everywhere, Private - accessible from only the same namespace and class
        [("str", "name")], // argument list, pairs of ("type", "argument_name")
        false, // false - checks argument count before function execution, true - doesn't check and fills missing spots with null
        // source code:
        [
            "!sys->io::print \"Hello, \" $name",
            "!sys->io::print_line \"!\""
        ],
        "greet.lt" // file name
    );
    ```
2. Append it to the runtime container:
    ```cs
    rtContainer.AppendDFunc(function);
    ```
   > **NOTE:** Although there is a `DFuncs` property in a runtime container, do not manually append to it. (To be honest, I've made this sound threatening, but worst case scenario: your function won't be found.)
3. Congrats, it can now be called from Lifetime!

## Create and append Lifetime variables from C# (`LTVar`)
1. Create the variable:
    ```cs
    LTVar myVar = LTVar.SimpleMut(
        "str", // type
        "my_var", // name
        "Hello, world!", // value
        "my_namespace", // namespace
        "my_class" // class
    );
    // for const values, use:
    LTVar myConst = LTVar.SimpleConst(
        "str", // type
        "my_const", // name
        "This is a constant string. It can be read, but not written to.", // value
        "my_namespace", // namespace
        "my_class" // class
    );
    ```
2. Append it to the runtime container:
    ```cs
    // to append one variable:
    rtContainer.Vars.Add(myVar);
    // to append multiple variables:
    rtContainer.Vars.AddRange([myVar1, myVar2, myVar3]);
    ```
3. Congrats, it can now be used from Lifetime!

## Execute Lifetime functions from a runtime container from C#
### (Recommended) Method 1: Just use `Exec`
```cs
if (LTInterpreter.Exec(["!my_namespace::my_class->greet \"John\""], "greeting.lt", ref rtContainer)) {
    Console.WriteLine("yay success!!");
}
else {
    Console.WriteLine("epic fail");
}
```
### Method 2: Use `FindAndExecFunc`
> **NOTE:** This is a function usually just meant for internal function lookup, but it can be used for this.
```cs
LTError? e = FindAndExecFunc(
    "!my_namespace::my_class->greet", // function name
    "\"John Doe\"".Split(' '), // argument list as a whole string split by spaces
    "greeting.lt", // file name
    "!my_namespace::my_class->greet \"John Doe\"", // line contents (now you see why this is internal)
    1, // line number
    ref rtContainer // reference to the container
);
if (e == null) {
    Console.WriteLine("yay success!!");
}
else {
    Console.WriteLine("epic fail");
    // handle the error data appropriately...
}
```
### (Hacky) Method 3: Get and call the function directly
1. Get it:
    ```cs
    // use IFuncs and LTInternalFunc if internal function or DFuncs and LTDefinedFunc if defined function
    LTInternalFunc function = rtContainer.IFuncs["my_namespace/my_class/greet"]; // format: namespace/class/function_name
    ```
2. Call it:
    ```cs
    // if errorStr is null, all is ok
    // if it isn't, discard returnValue and handle the error your way
    (LTVar? returnValue, string? errorStr) = function.Call(
        ref rtContainer,
        new([LTVar.SimpleMut("str", "", "John", "", "")]) // arguments
    );
    ```
3. Bop it
4. Twist it
5. Pull it

## Get Lifetime variables from C#
```cs
if (rtContainer.Vars.TryGetValue("my_namespace", "my_class", "my_var", out LTVar myVar)) {
    // ...
}
```
