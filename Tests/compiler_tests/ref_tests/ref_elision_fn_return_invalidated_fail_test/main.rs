fn input[T:! type](dd: &mut T) -> &mut T {
    dd
}
fn main() -> i32 {
    let num = 5;
    let derived = input(&mut num);
    &mut num;
    *derived
}
