Made a programming language that is compiled to machine code using LLVM. the syntax is similar to that of rust
Here is some example code
```rust

use code::Human;

use std::primitive::i32;
use code::make_human_with_age;
struct Human{
    age: i32   
}
fn make_human_with_age(inputtedAge: i32) -> Human{
    Human{
        age = inputtedAge  
    }
}


fn main() -> i32{
    let createdHuman : Human = make_human_with_age(5);
    createdHuman.age = createdHuman.age * 2;
    createdHuman.age
}

```
