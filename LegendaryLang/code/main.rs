
use code::Human;
use code::dark::Nester;
 use std::primitive::i32;


fn main() -> i32{
    code::main2()

}

fn main2() -> i32{
    let a = code::Human{
            age = 1 + 1
        };
    a.age = a.age * 2;
    a.age
}

