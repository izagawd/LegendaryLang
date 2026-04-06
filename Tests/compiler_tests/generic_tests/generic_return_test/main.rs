
fn kk(T:! type, bruh: T) -> T{
    if (true){
        bruh
    } else{
         bruh
        }
   
}

fn dd(T:! type, idk: T) -> T{
    return kk(T, idk);
    }
fn main() -> i32{
    let bruh = dd(i32, 5);
    let bruh2  = dd(bool, true);
    if(bruh2){
        kk(i32, 5)
        } else {
            kk(i32, 10)
            }
}

