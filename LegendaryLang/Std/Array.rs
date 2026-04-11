impl[T:! Sized, U:! usize] [T; U] {
    fn get_ref(self: &Self, index: usize) -> Option(&T);
    fn get_mut(self: &mut Self, index: usize) -> Option(&mut T);
}
