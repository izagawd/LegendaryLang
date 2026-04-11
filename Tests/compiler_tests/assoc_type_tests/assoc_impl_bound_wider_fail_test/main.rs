trait Maker: Sized {
    let Output :! Sized;
}

impl Maker for i32 {
    let Output :! type = i32;
}

fn main() -> i32 { 0 }
