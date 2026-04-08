trait Idk: Sized {
    fn add(self: &Self, input: Self) -> Self;
}
impl Idk for i32 {
    fn add(self: &Self, input: Self) -> Self {
        input + *self
    }
}
fn main() -> i32 {
    let num = 5;
    num.add(10)
}
