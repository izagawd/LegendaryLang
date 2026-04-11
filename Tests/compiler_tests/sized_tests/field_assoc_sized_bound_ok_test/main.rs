trait Maker: Sized {
    let Output :! Sized +Sized;
    fn make(self: Self) -> Self.Output;
}

struct Container(T:! Sized +Maker) {
    val: (T as Maker).Output
}

fn main() -> i32 { 0 }
