struct Wrapper<T> {
    val: T
}

impl<T: Copy> Copy for Wrapper<T> {}

fn idk<T: Copy>(input: T) -> i32 {
    let made = Wrapper::<T> { val = input };
    let move_here = made;
    let should_copy = made;
    5
}

fn main() -> i32 {
    idk::<i32>(10)
}
