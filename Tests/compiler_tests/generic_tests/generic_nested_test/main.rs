

fn kk(T:! Sized, bruh: i32) -> i32{
    if (bruh > 5){
        bruh = bruh - 1;
        dd(T, bruh)
    } else{
         bruh
        }
   
}

fn dd(T:! Sized, idk: i32) -> i32{
    return kk(T, idk);
    }
fn main() -> i32{
    kk(bool, 2) + kk(i32, 5)
}

