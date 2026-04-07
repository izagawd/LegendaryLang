trait Foo {
    let Bruh :! type;
    fn yo() -> Self.Bruh;
}

impl Foo for i32 {
    let Bruh :! type = i32;
    fn yo() -> Self.Bruh { 5 }
}

fn main() -> i32 {
    i32.yo()
}
