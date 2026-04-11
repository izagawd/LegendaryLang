trait Foo {
    let Bruh :! Sized;
    fn yo() -> Bruh;
}

impl Foo for i32 {
    let Bruh :! Sized = i32;
    fn yo() -> i32 { 5 }
}

fn main() -> i32 {
    i32.yo()
}
