
use code::Human;
use code::hello;
use code::bruh;
use std::primitive::i32;
use code::make_human_with_age;


fn hello<T>(kk: T) -> T{
    kk
}

fn bruh<T>(){
    hello::<T>();    
}

fn main() -> i32{
  let kk =  hello::<i32>(4) + 5;
  kk
}

