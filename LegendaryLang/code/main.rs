
use code::Human;
use code::hello;
use code::bruh;
use std::primitive::i32;
use code::make_human_with_age;




fn main() -> i32{
  let kk =  hello::<i32>(4) + 5;
  kk
}

fn hello<T>(kk: T) -> i32{
    5
}

fn bruh<T>(){
    hello::<T>();    
}