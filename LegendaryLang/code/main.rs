
use code::Human;
use code::hello;
use code::bruh;
use std::primitive::i32;
use code::make_human_with_age;
use code::Idk;

struct Idk{
    kk: i32}
   
   
 fn bruh() -> Idk{
    let kk =if false{
        Idk{kk = 5} 
    }   else if false{
        Idk{kk = 10}
        } else{
            Idk{kk = 22}};
        kk

  } 
fn main() -> i32{
    let d : i32= bruh().kk;
    d
}

