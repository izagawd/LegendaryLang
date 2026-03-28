trait Idk {
    fn add(self_val: Self, input: Self) -> Self;
}
impl Idk for i32 {
    fn add(self_val: Self, input: Self) -> Self {
        input + self_val
    }
}
fn main() -> i32 {
    <i32 as Idk>::add(5, 10)
}
