
use code::Human;
use code::dark::Nester;
 use std::primitive::i32;


fn bruh<T,U>(dd: T) -> T{
    dd
    }
fn hello<T,U>(dd: T) -> T{
        code::bruh::<T,U>(dd)
    }
fn main() -> i32{
    code::hello::<code::dark::Nester, i32>(code::dark::Nester{
            humanNested = code::Human{
                age = 5
                }
            }).humanNested.age

}

