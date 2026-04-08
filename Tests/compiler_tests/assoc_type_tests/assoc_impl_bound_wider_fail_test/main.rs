trait Maker: Sized {
    let Output :! type;
}

impl Maker for i32 {
    let Output :! MetaSized = i32;
}

fn main() -> i32 { 0 }
