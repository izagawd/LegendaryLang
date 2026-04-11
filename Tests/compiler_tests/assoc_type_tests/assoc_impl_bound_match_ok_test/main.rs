trait Maker: Sized {
    let Output :! Sized;
}

impl Maker for i32 {
    let Output :! Sized = u8;
}

fn main() -> i32 { 0 }
