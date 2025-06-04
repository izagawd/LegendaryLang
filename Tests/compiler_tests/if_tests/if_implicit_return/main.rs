
use std::primitive::i32;
fn main() -> i32{
    let gotten = if true{
        5
        }
    else{
        10
        };
        
    let gotten2 = if false{
            7
        }   else{
            4
            };
            
    gotten + gotten2  
}