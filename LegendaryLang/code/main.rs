
use code::Human;
use code::hello;
use code::bruh;
use std::primitive::i32;
use code::make_human_with_age;




fn main() -> i32{
  code::get_left::<i32>(5 + 5, 7 )
}

fn get_left<T>(left: T, right: T) -> T{
    left
}