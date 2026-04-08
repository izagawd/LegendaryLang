trait Producer: Sized {
    let Item :! MetaSized;
    fn produce(self: Self) -> &Item;
}

struct Holder[T:! Producer] {
    ptr: &(T as Producer).Item
}

fn main() -> i32 { 0 }
