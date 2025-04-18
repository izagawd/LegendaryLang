
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

