trait Maker: Sized {
    let Output :! Sized;
    fn make(self: Self) -> Self.Output;
}

struct Container(T:! Maker) {
    val: (T as Maker).Output
}

fn main() -> i32 { 0 }
