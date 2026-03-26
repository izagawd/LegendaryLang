fn do_sub<T: Sub<T, Output = i32> + Copy>(a: T, b: T) -> <T as Sub<T>>::Output {
    a - b
}

fn main() -> i32 {
    do_sub(10, 3)
}
