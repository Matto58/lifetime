# Namespace `sys`
## Class `sys->io`
### `!sys->io::print`
Prints the specified messages to the console, each argument printed back-to-back.
#### Arguments (argument count is ignored) (none)
#### Example
```
!sys->io::print "my message!"
```
### `!sys->io::print_line`
Prints the specified messages to the console, adding a new line at the end, each argument printed on separate lines.
#### Arguments (argument count is ignored) (none)
#### Example
```
!sys->io::print_line "hello, world!"
```
### `!sys->io::read_line`
Prints the specified string to the console without a new line, then reads input.
#### Arguments
* str question - The string to print.
#### Returns (`str`)
The input.
#### Example
```
let str name !sys->io::read_line "what's your name? "
!sys->io::print "hello, " $name "!"
!sys->io::print_line
```
## Class `sys->tools`
### `!sys->tools::is_null`
Checks if the specified object is null.
#### Arguments
* obj object - The object to check.
#### Returns (`bool`)
If the object is null.
#### Example
```
let str my_string "meoww mrrow mrrrrp :3"
if !sys->tools::is_null $my_string then
    !sys->io::print_line "is null!"
end
```
### `!sys->tools::split_str`
Splits a string into a string array.
#### Arguments
* str string - The string to split.
* str separator - The string separator.
#### Returns (`str_array`)
The string array.
#### Example
```
let str my_string ":3 :3 :3 :3 :3 :3 :3 :3"
let str_array array !sys->tools::split_str $my_string
!sys->test::print_line_arr $array
```
### `!sys->tools::create_array`
Creates a string array with the specified strings.
#### Arguments (argument count is ignored) (none)
#### Returns (`obj`)
The string array.
#### Example
```
let obj my_array !sys->tools::create_array "meow" "mrrp" "mrrow"
!sys->test::print_line_arr $my_array
```
## Class `sys->dev`
### `!sys->dev::bindns`
Makes the specified namespace be usable globally.
#### Arguments
* str namespace - The namespace.
#### Example
```
!sys->dev::bindns "sys"
!io::print_line "hi!!!"
!dev::unbindns "sys"
```
### `!sys->dev::unbindns`
Does the opposite of bindns.
#### Arguments
* str namespace - The namespace. 
#### Example
```
!sys->dev::bindns "sys"
!io::print_line "hi!!!"
!dev::unbindns "sys"
```
## Class `sys->fl`
### `!sys->fl::open_r`
Opens a file for reading.
#### Arguments
* str filename - The file name.
#### Returns (`int32`)
A handle index for the file.
#### Example
```
let int32 hFile = !sys->fl::open_r "myfile.txt"
# ...
!sys->fl::close $hFile
```
### `!sys->fl::open_w`
Opens a file for writing.
#### Arguments
* str filename - The file name.
#### Returns (`int32`)
A handle index for the file.
#### Example
```
let int32 hFile = !sys->fl::open_w "myfile.txt"
# ...
!sys->fl::close $hFile
```
### `!sys->fl::open_rw`
Opens a file for reading and writing.
#### Arguments
* str filename - The file name.
#### Returns (`int32`)

#### Example
```
let int32 hFile = !sys->fl::open_rw "myfile.txt"
# ...
!sys->fl::close $hFile
```
### `!sys->fl::close`
Closes the specified file.
#### Arguments
* int32 handle - The file's handle index.
#### Example
```
!sys->fl::close $hFile
```
### `!sys->fl::read_as_str`
Reads the specified file as a string.
#### Arguments
* int32 handle - The file's handle index.
#### Returns (`str`)
The entire contents of the file.
#### Example
```
let int32 hFile = !sys->fl::open_r "myfile.txt"
!sys->io::print_line !sys->fl::read_as_str $hFile
!sys->fl::close $hFile
```
### `!sys->fl::enum_dir`
Gets all files in a directory.
#### Arguments
* str path - The path to the directory
#### Returns (`obj`)
An array containing all the file names of the directory.
#### Example
```
!sys->test::print_line_arr !sys->fl::enum_dir "mydir"
```
## Class `sys->test`
### `!sys->test::ret_true`
Returns true.
#### Arguments (none)
#### Returns (`bool`)
The boolean value true.
### `!sys->test::ret_false`
Returns false.
#### Arguments (none)
#### Returns (`bool`)
The boolean value false.
### `!sys->test::print_line_arr`
Prints the specified string array on individual lines.
#### Arguments
* obj arr - The string array.
## Class `sys->rt`
### `!sys->rt::lt_ver`
Returns the current Lifetime runtime version.
#### Arguments (none)
#### Returns (`str`)
The current Lifetime runtime version, for example "0.7.0 (beta)".
#### Example
```
!sys->io::print "running on version " !sys->rt::lt_ver
!sys->io::print_line ""
```
## Class `sys->error`
### `!sys->error::get_message`
Gets the error message of the last caught error.
#### Arguments (none)
#### Returns (`str`)
The error message.
### `!sys->error::get_line_num`
Gets the line number of where the last caught error occured.
#### Arguments (none)
#### Returns (`int32`)
The line number.
### `!sys->error::get_line_content`
Gets the content of the line of where the last caught error occured.
#### Arguments (none)
#### Returns (`str`)
The line content.
