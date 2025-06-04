use std::primitive::i32;
use code::bruh;
use code::hello;
use code::Gay;
struct Gay{
    kk: i32
 }

fn bruh<T>(kk: T) -> T{
        if false{
            hello::<T>(kk)
            }else{
                kk
        }
    }
fn hello<T>(kk: T) -> T{
    
        bruh::<T>(kk)
    
    }
fn main() -> i32{
    let idk = Gay{kk = 5};
    hello::<i32>(idk.kk)
}

