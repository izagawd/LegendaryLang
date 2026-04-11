trait Producer: Sized {
    let Item :! type;
}

impl Producer for i32 {
    let Item :! Sized = str;
}

fn main() -> i32 { 0 }
