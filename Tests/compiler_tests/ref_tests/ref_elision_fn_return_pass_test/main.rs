fn input[T:! Sized](dd: &mut T) -> &mut T {
    dd
}
fn main() -> i32 {
    let num = 5;
    let derived = input(&mut num);
    *derived
}
