struct Idk['a](T:! Sized) {
    dd: &'a mut T
}

fn main() -> i32 {
    let a = GcMut.New(5);
    let b = make Idk { dd: &mut *a };
    *b.dd
}
