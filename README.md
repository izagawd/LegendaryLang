Made a programming language that is compiled to machine code using LLVM. the syntax is similar to that of rust,
only difference being to initialize a struct fields you ue "=" instead of "'"
Here is some example code
```rust
// making a struct
struct Human{
    age: i32   
}

fn make_human_with_age(inputtedAge: i32) -> Human{
    Human{
        age = inputtedAge  // unlike rust, we use = instead of :  when assigning fields to a struct during construction
    }
}

fn implicit_return() -> i32{
    let createdHuman : Human = make_human_with_age(5);
    createdHuman.age = createdHuman.age * 2;
    createdHuman.age
    // last value is returned if a semi colon is not after it
}
fn explicit_return() -> i32{
    // normal return statement also works
    return 5;
}
fn ifs() -> i32{
    if (true){
        return 5;
    } else{
        return 10;
    }
}
fn loops(input: i32) -> i32{
    
    while(input < 0){
        input = input - 1;
    }
    input // or "return input';"
}
// functions can also return void

fn returning_void() {}
```
