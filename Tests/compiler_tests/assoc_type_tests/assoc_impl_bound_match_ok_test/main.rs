trait Maker: Sized {
    let Output :! type;
}

impl Maker for i32 {
    let Output :! type = u8;
}

fn main() -> i32 { 0 }
