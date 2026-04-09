struct Idk['a](T:! type) {
    dd: &'a uniq T
}

fn main() -> i32 {
    let a = Box.New(5);
    let b = make Idk { dd: &uniq *a };
    *b.dd
}
