
use code::Human;
use code::hello;
use code::bruh;
use std::primitive::i32;
use code::make_human_with_age;
use code::Idk;
use code::other;
struct Idk{
    kk: i32}
   
   fn other(kk: Idk) -> Idk{
       
       Idk{kk = kk.kk + 4}
      }
 fn bruh() -> Idk{
    let a  = if true{
        
      5       
         }
     else if false{
        10
        } else{
            11
     };
    other(Idk{kk = a})
} 
fn main() -> i32{
     bruh().kk
}

