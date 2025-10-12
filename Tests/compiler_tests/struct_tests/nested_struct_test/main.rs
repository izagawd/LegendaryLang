struct Bro{val: i32}
struct Dd{ bro: Bro}
fn kk<T>(bruh: T) -> T{
    if (true){
        bruh
    } else{
         bruh 
        }
   
}


fn main() -> i32{
    let gotten = Dd{bro = Bro{val = 5}};
    gotten.bro.val + 10
}

